using Amazon.CognitoIdentityProvider.Model;
using ReportSender.Entities;
using ReportSender.Entities.Reports;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReportSender;

public static class Mapper
{
    public static ReportHistoryLocation ToLocationReportsAsync(
        this List<DailyAggregate> reports,
        List<DailyAggregate> previousPeriodReports,
        DateOnly startDate,
        DateOnly endDate,
        string locationName)
    {
        var deltaRevenue = 0.0;
        var deltaAvgCuisineFeedback = 0.0;

        if (previousPeriodReports.Count != 0)
        {
            deltaRevenue = (double)Math.Round(
                (reports.Sum(r => r.TotalRevenue) - previousPeriodReports.Sum(r => r.TotalRevenue)) /
                    previousPeriodReports.Sum(r => r.TotalRevenue) * 100, 2);

            deltaAvgCuisineFeedback = Math.Round(
                (reports.Average(r => r.AverageCuisineRating) - previousPeriodReports.Average(r => r.AverageCuisineRating)) /
                    previousPeriodReports.Average(r => r.AverageCuisineRating) * 100, 2);
        }

        return new ReportHistoryLocation
        {
            Id = Guid.NewGuid().ToString(),
            ReportType = ReportType.Sales,
            StartDate = startDate,
            EndDate = endDate,
            LocationId = reports.First().LocationId,
            LocationName = locationName,
            TotalOrders = reports.Sum(r => r.OrdersProcessed),
            AvgCuisineFeedback = Math.Round(reports.Average(r => r.AverageCuisineRating), 2),
            MinCuisineFeedback = reports.Min(r => r.MinimumCuisineRating),
            Revenue = Math.Round((double)reports.Sum(r => r.TotalRevenue), 2),
            DeltaAvgCuisineFeedback = deltaAvgCuisineFeedback,
            DeltaRevenue = deltaRevenue,
        };
    }

    public static ReportHistoryWaiter ToWaiterReportsAsync(
        this List<DailyAggregate> reports,
        List<DailyAggregate> previousPeriodReports,
        DateOnly startDate,
        DateOnly endDate,
        UserType? waiter)
    {
        var deltaAvgServiceFeedback = 0.0;
        var deltaTotalOrders = 0.0;

        if (previousPeriodReports.Count != 0)
        {
            deltaAvgServiceFeedback = Math.Round(
                (reports.Average(r => r.AverageServiceRating) - previousPeriodReports.Average(r => r.AverageServiceRating))
                    / previousPeriodReports.Average(r => r.AverageServiceRating) * 100, 2);

            deltaTotalOrders = Math.Round(
                ((double)(reports.Sum(r => r.OrdersProcessed) - previousPeriodReports.Sum(r => r.OrdersProcessed))
                    / previousPeriodReports.Sum(r => r.OrdersProcessed) * 100), 2);
        }

        var fullName = waiter?.Attributes.FirstOrDefault(a => a.Name == "given_name")?.Value ?? "Unknown"
            + " " + waiter?.Attributes.FirstOrDefault(a => a.Name == "family_name")?.Value ?? "";

        return new ReportHistoryWaiter
        {
            Id = Guid.NewGuid().ToString(),
            ReportType = ReportType.Service,
            StartDate = startDate,
            EndDate = endDate,
            WaiterId = reports.First().WaiterId,
            WaiterName = fullName,
            WaiterEmail = waiter.Attributes.FirstOrDefault(a => a.Name == "email")?.Value ?? "Unknown",
            WorkingHours = Math.Round(reports.Sum(r => r.HoursWorked), 2),
            TotalOrders = reports.Sum(r => r.OrdersProcessed),
            AvgServiceFeedback = Math.Round(reports.Average(r => r.AverageServiceRating), 2),
            MinServiceFeedback = reports.Min(r => r.MinimumServiceRating),
            DeltaAvgServiceFeedback = deltaAvgServiceFeedback,
            DeltaTotalOrders = deltaTotalOrders,
        };
    }
}