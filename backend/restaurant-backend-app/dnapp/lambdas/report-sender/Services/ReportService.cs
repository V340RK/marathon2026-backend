using Amazon;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Core;
using ReportSender.Entities;
using ReportSender.Entities.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReportSender.Services;

public class ReportService : IReportService
{
    private readonly string _tableName = Environment.GetEnvironmentVariable("ReportTableName")
        ?? throw new InvalidOperationException("ReportsTableName environment variable is not set.");

    private static readonly string Region = Environment.GetEnvironmentVariable("Region")
        ?? throw new InvalidOperationException("Region environment variable is not set.");

    private readonly IDynamoDBContext _dynamoDbContext = new DynamoDBContextBuilder()
        .WithDynamoDBClient(() => new Amazon.DynamoDBv2.AmazonDynamoDBClient(RegionEndpoint.GetBySystemName(Region)))
        .Build();

    private readonly ILocationService _locationService = new LocationService();
    private readonly IUserService _userService = new UserService();

    public async Task<List<ReportHistoryLocation>> GetLocationsReportAsync(DateOnly startDate, DateOnly endDate, ILambdaLogger logger)
    {
        var (currentPeriodReports, previousPeriodReportsList) = await GetForPeriodAsync(startDate, endDate, logger);

        var groupedByLocation = currentPeriodReports.GroupBy(r => r.LocationId).ToDictionary(g => g.Key, g => g.ToList());
        var previousGroupedByLocation = previousPeriodReportsList.GroupBy(r => r.LocationId).ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<ReportHistoryLocation>();

        foreach (var (locationId, locationReports) in groupedByLocation)
        {
            var locationName = await _locationService.GetLocationAddressByIdAsync(locationId);
            var previousReports = previousGroupedByLocation.TryGetValue(locationId, out var previous)
                ? previous : [];

            var report = locationReports.ToLocationReportsAsync(previousReports, startDate, endDate, locationName);
            result.Add(report);
        }

        return result;
    }

    public async Task<List<ReportHistoryWaiter>> GetWaitersReportAsync(DateOnly startDate, DateOnly endDate, ILambdaLogger logger)
    {
        var (currentPeriodReports, previousPeriodReportsList) = await GetForPeriodAsync(startDate, endDate, logger);

        var groupedByWaiter = currentPeriodReports.GroupBy(r => r.WaiterId).ToDictionary(g => g.Key, g => g.ToList());
        var previousGroupedByWaiter = previousPeriodReportsList.GroupBy(r => r.WaiterId).ToDictionary(g => g.Key, g => g.ToList());

        var waiters = await _userService.GetUsersByGroupAsync(Roles.Waiter.ToString());
        var result = new List<ReportHistoryWaiter>();

        foreach (var (waiterId, reports) in groupedByWaiter)
        {
            var waiter = waiters.FirstOrDefault(w => w.Attributes.Any(a => a.Name == "sub" && a.Value == waiterId));
            var previousReports = previousGroupedByWaiter.TryGetValue(waiterId, out var previous)
                ? previous : [];

            var report = reports.ToWaiterReportsAsync(previousReports, startDate, endDate, waiter);
            result.Add(report);
        }

        return result;
    }

    private async Task<(List<DailyAggregate>, List<DailyAggregate>)> GetForPeriodAsync(DateOnly from, DateOnly to, ILambdaLogger logger)
    {
        var currentPeriodReports = await GetForPeriodAsync(
            from.ToDateTime(TimeOnly.MinValue),
            to.ToDateTime(TimeOnly.MaxValue));

        logger.LogLine($"[DEBUG] Retrieved {currentPeriodReports.Count} reports from DynamoDB for period {from} to {to}");

        var periodLength = to.DayNumber - from.DayNumber + 1;
        var previousStartDate = from.AddDays(-periodLength);
        var previousEndDate = from.AddDays(-1);

        var previousPeriodReportsList = await GetForPeriodAsync(
            previousStartDate.ToDateTime(TimeOnly.MinValue),
            previousEndDate.ToDateTime(TimeOnly.MaxValue));

        logger.LogLine($"[DEBUG] Retrieved {previousPeriodReportsList.Count} previous reports from {previousStartDate} to {previousEndDate}");

        return (currentPeriodReports, previousPeriodReportsList);
    }

    private async Task<List<DailyAggregate>> GetForPeriodAsync(DateTime from, DateTime to)
    {
        var config = new ScanConfig { OverrideTableName = _tableName, ConsistentRead = true };
        var conditions = new List<ScanCondition> { new(nameof(DailyAggregate.Date), ScanOperator.Between, from, to) };

        var scan = _dynamoDbContext.ScanAsync<DailyAggregate>(conditions, config);
        return await scan.GetRemainingAsync();
    }
}