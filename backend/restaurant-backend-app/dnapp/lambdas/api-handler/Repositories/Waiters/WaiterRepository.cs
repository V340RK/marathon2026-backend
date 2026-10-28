using Amazon.DynamoDBv2.DataModel;
using APIHandler.Constants;
using APIHandler.Models.WaiterList;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Repositories.Waiters;

public class WaiterRepository : IWaiterRepository
{
    private readonly string _waitersTableName = ResourceNames.WaiterListTableName;
    private readonly string _waitersGlobalIndex = ResourceNames.WaitersGlobalIndex;

    private readonly IDynamoDBContext _dbContext;

    public WaiterRepository(IDynamoDBContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<WaiterListItem?> GetWaiterByIdAsync(string userId)
    {
        return await _dbContext.LoadAsync<WaiterListItem>(userId, DynamoConfig.LoadConfig(_waitersTableName));
    }

    public async Task<List<WaiterListItem>> GetWaitersByLocationIdAsync(string locationId)
    {
        var queryConfig = DynamoConfig.QueryConfig(_waitersTableName);
        queryConfig.IndexName = _waitersGlobalIndex;
        queryConfig.ConsistentRead = false;

        return await _dbContext.QueryAsync<WaiterListItem>(locationId, queryConfig).GetRemainingAsync();
    }

    public async Task<List<WaiterListItem>> GetAllWaitersAsync()
    {
        var scanConfig = DynamoConfig.ScanConfig(_waitersTableName);
        return await _dbContext.ScanAsync<WaiterListItem>([], scanConfig).GetRemainingAsync();
    }

    public async Task DeleteWaiterAsync(WaiterListItem waiter)
    {
        await _dbContext.DeleteAsync(waiter, DynamoConfig.DeleteConfig(_waitersTableName));
    }

    public async Task SaveWaiterAsync(WaiterListItem waiter)
    {
        await _dbContext.SaveAsync(waiter, DynamoConfig.SaveConfig(_waitersTableName));
    }
}