using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using APIHandler.Constants;
using APIHandler.Entities;
using APIHandler.Entities.Reports;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Repositories.Reports;

public class ReportRepository : IReportRepository
{
    private readonly string _reportsTableName = ResourceNames.ReportsTableName;
    private readonly string _reportsHistoryTableName = ResourceNames.ReportsHistoryTableName;
    private readonly string _reportsHistoryUserIdIndex = ResourceNames.ReportsHistoryTableUserIdGlobalIndex;

    private readonly IDynamoDBContext _dbContext;

    public ReportRepository(IDynamoDBContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<List<DailyAggregate>> GetAggregatesForPeriodAsync(DateTime startDateTime, DateTime endDateTime, string locationId)
    {
        var conditions = new List<ScanCondition>
        {
            new(nameof(DailyAggregate.Date), ScanOperator.Between, startDateTime, endDateTime),
            new(nameof(DailyAggregate.LocationId), ScanOperator.Equal, locationId)
        };

        return await _dbContext.ScanAsync<DailyAggregate>(conditions,
            new ScanConfig()
            {
                OverrideTableName = _reportsTableName
            }).GetRemainingAsync();
    }

    public async Task<List<ReportHistoryWaiter>> GetWaiterReportsByUserIdAsync(string userId)
    {
        var queryConfig = new QueryConfig
        {
            IndexName = _reportsHistoryUserIdIndex,
            OverrideTableName = _reportsHistoryTableName
        };

        return await _dbContext.QueryAsync<ReportHistoryWaiter>(userId, queryConfig).GetRemainingAsync();
    }

    public async Task<List<ReportHistoryLocation>> GetLocationReportsByUserIdAsync(string userId)
    {
        var queryConfig = new QueryConfig
        {
            IndexName = _reportsHistoryUserIdIndex,
            OverrideTableName = _reportsHistoryTableName
        };

        return await _dbContext.QueryAsync<ReportHistoryLocation>(userId, queryConfig).GetRemainingAsync();
    }

    public async Task SaveReportHistoryAsync<T>(T report) where T : ReportHistoryBase
    {
        await _dbContext.SaveAsync(report, new SaveConfig()
        {
            OverrideTableName = _reportsHistoryTableName,
        });
    }
}