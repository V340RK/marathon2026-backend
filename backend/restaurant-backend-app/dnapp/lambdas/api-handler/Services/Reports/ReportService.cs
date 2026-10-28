using Amazon.Lambda.Core;
using Amazon.SQS;
using Amazon.SQS.Model;
using APIHandler.Constants;
using APIHandler.Constants.Reports;
using APIHandler.Entities;
using APIHandler.Entities.Reports;
using APIHandler.Exceptions;
using APIHandler.Models.Feedbacks;
using APIHandler.Models.Reports;
using APIHandler.Repositories.Reports;
using APIHandler.Services.Auth;
using APIHandler.Services.Dishes;
using APIHandler.Services.Feedbacks;
using APIHandler.Services.Locations;
using APIHandler.Services.Orders;
using APIHandler.Services.Reports.Interfaces;
using APIHandler.Services.Reservations;
using APIHandler.Services.Waiters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APIHandler.Services.Reports;

public class ReportService : IReportService
{
    private readonly string _reportQueueName = ResourceNames.ReportQueueName;

    private readonly IReservationService _reservationService;
    private readonly IWaiterService _waiterService;
    private readonly IFeedbackService _feedbackService;
    private readonly IOrderService _orderService;
    private readonly IDishService _dishService;
    private readonly IUserService _userService;
    private readonly ILocationService _locationService;
    private readonly AmazonSQSClient _sqsClient;
    private readonly IReportRepository _reportRepository;

    public ReportService(
        IReservationService reservationService,
        IWaiterService waiterService,
        IFeedbackService feedbackService,
        IOrderService orderService,
        IDishService dishService,
        IUserService userService,
        ILocationService locationService,
        AmazonSQSClient sqsClient,
        IReportRepository reportRepository)
    {
        _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
        _waiterService = waiterService ?? throw new ArgumentNullException(nameof(waiterService));
        _feedbackService = feedbackService ?? throw new ArgumentNullException(nameof(feedbackService));
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _dishService = dishService ?? throw new ArgumentNullException(nameof(dishService));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
        _sqsClient = sqsClient ?? throw new ArgumentNullException(nameof(sqsClient));
        _reportRepository = reportRepository ?? throw new ArgumentNullException(nameof(reportRepository));
    }

    public async Task<Report> GenerateReportAsync(string reservationId)
    {
        var reservation = await _reservationService.GetReservationByIdAsync(reservationId)
                          ?? throw new ArgumentException($"Reservation with ID {reservationId} not found.");
        if (reservation.Status != ReservationStatus.Completed)
        {
            throw new ValidationException(
                $"Reservation with ID {reservationId} is not completed. Unable to generate report.");
        }

        var waiter = await _waiterService.GetWaiterByReservationIdAsync(reservationId)
                     ?? throw new ValidationException($"Waiter for reservation {reservationId} not found.");
        string locationId = waiter.LocationId;
        var feedback = await _feedbackService.GetFeedbackForReservationAsync(reservationId)
                       ?? throw new ValidationException($"Feedback for reservation {reservationId} not found.");
        var verviceRating = feedback.SingleOrDefault(f => f.Type == FeedbackType.Service)?.Rating ?? 0;
        var cuisineRating = feedback.SingleOrDefault(f => f.Type == FeedbackType.CuisineExperience)?.Rating ?? 0;
        var revenue = 0m;

        return new Report
        {
            ReservationId = reservationId,
            CreatedAt = DateTime.UtcNow,
            ReservationStart = reservation.StartTime,
            ReservationEnd = reservation.EndTime,
            WaiterId = waiter.UserId,
            LocationId = locationId,
            ServiceRating = verviceRating,
            CuisineRating = cuisineRating,
            Revenue = revenue
        };
    }

    public async Task SendReportToSqsAsync(Report report)
    {
        var queueUrl = (await _sqsClient.GetQueueUrlAsync(_reportQueueName)).QueueUrl
                       ?? throw new InvalidOperationException($"Queue with name {_reportQueueName} not found.");
        var messageBody = System.Text.Json.JsonSerializer.Serialize(report);
        var sendMessageRequest = new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = messageBody
        };
        var response = await _sqsClient.SendMessageAsync(sendMessageRequest);
        if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"Failed to send report to SQS. Queue: {_reportQueueName}. Queue URL: {queueUrl}. Status code: {response.HttpStatusCode}");
        }
    }

    public async Task<List<DailyAggregate>> GetAggregatesForPeriodAsync(DateOnly start, DateOnly end, string locationId)
    {
        var startDateTime = start.ToDateTime(TimeOnly.MinValue);
        var endDateTime = end.ToDateTime(TimeOnly.MaxValue);

        return await _reportRepository.GetAggregatesForPeriodAsync(startDateTime, endDateTime, locationId);
    }

    public async Task<List<ReportHistoryWaiter>> GenerateWaiterReportsAsync(List<DailyAggregate> aggregates,
        DateOnly start,
        DateOnly end,
        string userId,
        ILambdaLogger logger)
    {
        DateOnly prevStart, prevEnd;
        GetPreviousPeriod(start, end, out prevStart, out prevEnd);
        logger.LogInformation($"Getting previous report for period: {prevStart} - {prevEnd}");

        var previousAggregates = await GetAggregatesForPeriodAsync(prevStart, prevEnd, aggregates[0].LocationId);
        var prevForWaiters = previousAggregates
            .GroupBy(x => new { x.WaiterId, x.LocationId })
            .ToDictionary(g => g.Key, g => new
            {
                TotalOrders = g.Sum(x => x.OrdersProcessed),
                AvgService = g.Average(x => x.AverageServiceRating)
            });

        var location = await _locationService.GetLocationByIdAsync(aggregates[0].LocationId);
        var tasks = aggregates
            .GroupBy(a => new { a.WaiterId, a.LocationId })
            .Select(async g =>
            {
                var key = g.Key;

                var hasPrev = prevForWaiters.TryGetValue(key, out var prev);

                var totalOrders = g.Sum(x => x.OrdersProcessed);
                var totalHours = g.Sum(x => x.HoursWorked);
                var avgService = g.Average(x => x.AverageServiceRating);
                var minFeedback = g.Min(x => x.MinimumServiceRating);

                var deltaAvgService = (hasPrev && prev!.AvgService != 0)
                    ? (avgService / prev.AvgService) * 100
                    : 0;

                var deltaTotalOrders = (hasPrev && prev!.TotalOrders != 0)
                    ? (totalOrders / prev.TotalOrders) * 100
                    : 0;

                var waiterName = await _userService.GetUserName(g.Key.WaiterId);
                var waiterEmail = await _userService.GetUserEmail(g.Key.WaiterId);

                return new ReportHistoryWaiter
                {
                    Id = Guid.NewGuid().ToString(),
                    ReportType = ReportType.Service,
                    StartDate = start,
                    EndDate = end,
                    UserId = userId,
                    LocationId = g.Key.LocationId,
                    WaiterId = g.Key.WaiterId,
                    WaiterName = waiterName ?? "Unknown",
                    WaiterEmail = waiterEmail ?? "Unknown",
                    LocationName = location != null ? location.Address : "Unknown",
                    TotalOrders = totalOrders,
                    WorkingHours = totalHours,
                    AvgServiceFeedback = avgService,
                    MinServiceFeedback = minFeedback,
                    DeltaAvgServiceFeedback = deltaAvgService,
                    DeltaTotalOrders = deltaTotalOrders
                };
            });

        return (await Task.WhenAll(tasks)).ToList();
    }

    public async Task<List<ReportHistoryLocation>> GenerateLocationReportsAsync(
        List<DailyAggregate> aggregates,
        DateOnly start,
        DateOnly end,
        string userId,
        ILambdaLogger logger)
    {
        DateOnly prevStart, prevEnd;
        GetPreviousPeriod(start, end, out prevStart, out prevEnd);
        logger.LogInformation($"Getting previous report for period: {prevStart} - {prevEnd}");

        var previousAggregates = await GetAggregatesForPeriodAsync(prevStart, prevEnd, aggregates[0].LocationId);
        var prevForLocation = previousAggregates
            .GroupBy(x => x.LocationId)
            .ToDictionary(g => g.Key, g => new
            {
                TotalOrders = g.Sum(x => x.OrdersProcessed),
                Revenue = g.Sum(x => x.TotalRevenue),
                AvgCuisine = g.Average(x => x.AverageCuisineRating)
            });

        var location = await _locationService.GetLocationByIdAsync(aggregates[0].LocationId);
        return aggregates
            .GroupBy(x => x.LocationId)
            .Select(g =>
            {
                var key = g.Key;
                var currentRevenue = g.Sum(x => x.TotalRevenue);
                var currentAvg = g.Average(x => x.AverageCuisineRating);

                var hasPrev = prevForLocation.TryGetValue(key, out var prev);
                var deltaRevenue = (hasPrev && prev!.Revenue != 0)
                    ? ((currentRevenue - prev.Revenue) / prev.Revenue) * 100
                    : 0;

                var deltaAvgCuisine = (hasPrev && prev!.AvgCuisine != 0)
                    ? ((currentAvg - prev.AvgCuisine) / prev.AvgCuisine) * 100
                    : 0;

                return new ReportHistoryLocation
                {
                    Id = Guid.NewGuid().ToString(),
                    ReportType = ReportType.Sales,
                    StartDate = start,
                    EndDate = end,
                    UserId = userId,
                    LocationId = g.Key,
                    LocationName = location != null ? location.Address : "Unknown",
                    TotalOrders = g.Sum(x => x.OrdersProcessed),
                    Revenue = g.Sum(x => x.TotalRevenue),
                    AvgCuisineFeedback = g.Average(x => x.AverageCuisineRating),
                    MinCuisineFeedback = g.Min(x => x.MinimumCuisineRating),
                    DeltaAvgCuisineFeedback = deltaAvgCuisine,
                    DeltaRevenue = deltaRevenue,
                };
            }).ToList();
    }

    public async Task SaveReportsHistoryAsync<T>(List<T> reports) where T : ReportHistoryBase
    {
        foreach (var report in reports)
        {
            await _reportRepository.SaveReportHistoryAsync(report);
        }
    }

    public async Task<List<UserReportsHistoryDto>> GetUserReportsListAsync(string userId)
    {
        var waiterReports = await _reportRepository.GetWaiterReportsByUserIdAsync(userId);
        var locationReports = await _reportRepository.GetLocationReportsByUserIdAsync(userId);

        var allReports = waiterReports.Cast<ReportHistoryBase>()
            .Concat(locationReports)
            .ToList();

        var grouped = allReports
            .GroupBy(r => (
                r.StartDate,
                r.EndDate,
                r.UserId,
                r.LocationId,
                r.ReportType
            ))
            .ToList();

        var result = new List<UserReportsHistoryDto>();

        foreach (var group in grouped)
        {
            var (start, end, user, locationId, reportType) = group.Key;
            var location = await _locationService.GetLocationByIdAsync(locationId);

            result.Add(new UserReportsHistoryDto
            {
                StartDate = start,
                EndDate = end,
                UserId = user,
                LocationId = locationId,
                LocationName = location?.Address ?? "Unknown",
                ReportType = reportType
            });
        }

        return result;
    }

    public async Task<List<ReportHistoryBase>?> QueryReportsHistoryAsync(
        string userId,
        ReportType reportType,
        string locationId,
        DateOnly startDate,
        DateOnly endDate)
    {
        switch (reportType)
        {
            case ReportType.Service:
                {
                    var serviceReports = await _reportRepository.GetWaiterReportsByUserIdAsync(userId);

                    var filtered = serviceReports
                        .Where(r =>
                            r.ReportType == reportType &&
                            r.LocationId == locationId &&
                            r.StartDate >= startDate &&
                            r.EndDate <= endDate)
                        .Cast<ReportHistoryBase>()
                        .ToList();

                return filtered;
            }

            case ReportType.Sales:
                {
                    var locationReports = await _reportRepository.GetLocationReportsByUserIdAsync(userId);

                    var filtered = locationReports
                        .Where(r =>
                            r.ReportType == reportType &&
                            r.LocationId == locationId &&
                            r.StartDate >= startDate &&
                            r.EndDate <= endDate)
                        .Cast<ReportHistoryBase>()
                        .ToList();

                return filtered;
            }

            default:
                throw new ArgumentException($"Unsupported report type: {reportType}");
        }
    }

    private static void GetPreviousPeriod(DateOnly start, DateOnly end, out DateOnly prevStart, out DateOnly prevEnd)
    {
        var totalDays = (end.ToDateTime(TimeOnly.MinValue) - start.ToDateTime(TimeOnly.MinValue)).Days + 1;
        prevStart = start.AddDays(-totalDays);
        prevEnd = start.AddDays(-1);
    }

    private async Task<decimal> CalculateTotalRevenue(List<OrderItem> items)
    {
        decimal totalRevenue = 0;
        foreach (var item in items)
        {
            var dish = await _dishService.GetDishByIdAsync(item.DishId);
            if (dish is not null)
            {
                totalRevenue += dish.Price * item.Quantity;
            }
        }

        return totalRevenue;
    }
}