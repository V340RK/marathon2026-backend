using ReportsHandler.Entities;
using System;
using System.Threading.Tasks;

namespace ReportsHandler.Services;

public interface IReportService
{
    Task<DailyAggregate> GetOrCreateDailyAggregateAsync(string waiterId, DateTime date);

    Task UpdateDailyAggregateAsync(DailyAggregate aggregate);
}