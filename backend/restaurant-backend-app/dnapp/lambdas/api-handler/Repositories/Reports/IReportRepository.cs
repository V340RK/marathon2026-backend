using APIHandler.Entities;
using APIHandler.Entities.Reports;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Repositories.Reports;

public interface IReportRepository
{
    Task<List<DailyAggregate>> GetAggregatesForPeriodAsync(DateTime startDateTime, DateTime endDateTime, string locationId);
    Task<List<ReportHistoryWaiter>> GetWaiterReportsByUserIdAsync(string userId);
    Task<List<ReportHistoryLocation>> GetLocationReportsByUserIdAsync(string userId);
    Task SaveReportHistoryAsync<T>(T report) where T : ReportHistoryBase;
}