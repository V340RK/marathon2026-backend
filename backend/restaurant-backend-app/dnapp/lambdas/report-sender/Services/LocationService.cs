using Amazon;
using Amazon.DynamoDBv2.DataModel;
using ReportSender.Entities.Locations;
using System;
using System.Threading.Tasks;

namespace ReportSender.Services;

public class LocationService : ILocationService
{
    private static readonly string LocationsTableName = Environment.GetEnvironmentVariable("LocationTableName")
        ?? throw new InvalidOperationException("LocationTableName environment variable is not set.");

    private static readonly string Region = Environment.GetEnvironmentVariable("Region")
        ?? throw new InvalidOperationException("Region environment variable is not set.");

    private readonly IDynamoDBContext _dynamoDbContext = new DynamoDBContextBuilder()
        .WithDynamoDBClient(() => new Amazon.DynamoDBv2.AmazonDynamoDBClient(RegionEndpoint.GetBySystemName(Region)))
        .Build();

    public async Task<string> GetLocationAddressByIdAsync(string locationId)
    {
        var loadConfig = new LoadConfig
        {
            ConsistentRead = true,
            OverrideTableName = LocationsTableName
        };

        var location = await _dynamoDbContext.LoadAsync<Location>(locationId, loadConfig);
        if (location is null)
        {
            throw new ArgumentException($"Location with ID {locationId} not found.");
        }

        return location.Address;
    }
}