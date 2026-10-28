using Amazon.DynamoDBv2.DataModel;
using APIHandler.Constants;
using APIHandler.Entities;
using APIHandler.Models.Feedbacks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Repositories.Locations;

public class LocationRepository : ILocationRepository
{
    private static readonly string LocationsTableName = ResourceNames.LocationsTableName;
    private static readonly string TablesTableName = ResourceNames.TablesTableName;
    private static readonly string FeedbackTableName = ResourceNames.FeedbackTableName;
    private static readonly string TablesLocationIdIndex = ResourceNames.TablesLocationIdIndex;
    private static readonly string FeedbackLocationIdIndex = ResourceNames.FeedbackGlobalIndex;

    private readonly IDynamoDBContext _dynamoDbContext;

    public LocationRepository(IDynamoDBContext dynamoDbContext)
    {
        _dynamoDbContext = dynamoDbContext ?? throw new ArgumentNullException(nameof(dynamoDbContext));
    }

    public async Task<Location?> GetLocationByIdAsync(string locationId)
    {
        return await _dynamoDbContext.LoadAsync<Location>(locationId, DynamoConfig.LoadConfig(LocationsTableName));
    }

    public async Task<Table?> GetTableByIdAsync(string tableId)
    {
        return await _dynamoDbContext.LoadAsync<Table>(tableId, DynamoConfig.LoadConfig(TablesTableName));
    }

    public async Task<IEnumerable<Location>> GetAllLocationsAsync()
    {
        return await _dynamoDbContext.ScanAsync<Location>(new List<ScanCondition>()).GetRemainingAsync();
    }

    public async Task<List<Table>> GetTablesByLocationIdAsync(string locationId)
    {
        var queryConfig = DynamoConfig.QueryConfig(TablesTableName);
        queryConfig.IndexName = TablesLocationIdIndex;
        queryConfig.ConsistentRead = false;

        return await _dynamoDbContext.QueryAsync<Table>(locationId, queryConfig).GetRemainingAsync();
    }

    public async Task<List<Feedback>> GetFeedbackByLocationIdAsync(string locationId)
    {
        var queryConfig = DynamoConfig.QueryConfig(FeedbackTableName);
        queryConfig.IndexName = FeedbackLocationIdIndex;
        queryConfig.ConsistentRead = false;

        return await _dynamoDbContext.QueryAsync<Feedback>(locationId, queryConfig).GetRemainingAsync();
    }

    public async Task SaveLocationAsync(Location location)
    {
        await _dynamoDbContext.SaveAsync(location, DynamoConfig.SaveConfig(ResourceNames.LocationsTableName));
    }

    public async Task DeleteLocationAsync(Location location)
    {
        await _dynamoDbContext.DeleteAsync(location, DynamoConfig.DeleteConfig(ResourceNames.LocationsTableName));
    }
}