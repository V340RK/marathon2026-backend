using Amazon;
using Amazon.DynamoDBv2.DataModel;
using ReportsHandler.Entities;
using System;
using System.Threading.Tasks;

namespace ReportsHandler.Services;

public class ReportService : IReportService
{
    private readonly string _tableName = Environment.GetEnvironmentVariable("ReportsTableName")
        ?? throw new InvalidOperationException("ReportsTableName environment variable is not set.");
    private static readonly string Region = Environment.GetEnvironmentVariable("AwsRegion")
        ?? throw new InvalidOperationException("AWS_REGION environment variable is not set.");

    private readonly IDynamoDBContext _dynamoDbContext = new DynamoDBContextBuilder()
        .WithDynamoDBClient(() => new Amazon.DynamoDBv2.AmazonDynamoDBClient(RegionEndpoint.GetBySystemName(Region)))
        .Build();

    public async Task<DailyAggregate> GetOrCreateDailyAggregateAsync(string waiterId, DateTime date)
    {
        var loadConfig = new LoadConfig { OverrideTableName = _tableName };
        var aggregate = await _dynamoDbContext.LoadAsync<DailyAggregate>(waiterId, date, loadConfig);
        if (aggregate is null)
        {
            aggregate = new DailyAggregate
            {
                WaiterId = waiterId,
                LocationId = string.Empty,
                Date = date,
                AverageServiceRating = 0,
                MinimumServiceRating = int.MaxValue,
                AverageCuisineRating = 0,
                MinimumCuisineRating = int.MaxValue,
                TotalRevenue = 0,
                HoursWorked = 0,
                OrdersProcessed = 0
            };
        }

        return aggregate;
    }

    public async Task UpdateDailyAggregateAsync(DailyAggregate aggregate)
    {
        var saveConfig = new SaveConfig { OverrideTableName = _tableName };
        await _dynamoDbContext.SaveAsync(aggregate, saveConfig);
    }
}