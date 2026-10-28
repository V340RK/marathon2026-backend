using Amazon.Lambda.Core;
using APIHandler.Constants.Reports;
using APIHandler.Entities;
using APIHandler.Entities.Reports;
using APIHandler.Models.Reports;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Services.Reports.Interfaces;

public interface IReportService
{
    Task<Report> GenerateReportAsync(string reservationId);

    Task SendReportToSqsAsync(Report report);

    Task<List<DailyAggregate>> GetAggregatesForPeriodAsync(DateOnly start, DateOnly end, string locationId);

    Task<List<ReportHistoryWaiter>> GenerateWaiterReportsAsync(List<DailyAggregate> aggregates,
        DateOnly start,
        DateOnly end,
        string userId,
        ILambdaLogger logger);

    Task<List<ReportHistoryLocation>> GenerateLocationReportsAsync(
        List<DailyAggregate> aggregates,
        DateOnly start,
        DateOnly end,
        string userId,
        ILambdaLogger logger);

    Task SaveReportsHistoryAsync<T>(List<T> reports) where T : ReportHistoryBase;

    Task<List<ReportHistoryBase>> QueryReportsHistoryAsync(
        string userId,
        ReportType reportType,
        string locationId,
        DateOnly startDate,
        DateOnly endDate);

    Task<List<UserReportsHistoryDto>> GetUserReportsListAsync(string userId);
}