using Amazon.DynamoDBv2.DataModel;
using System;

namespace ReportSender.Entities;

[DynamoDBTable("Reports")]
public class DailyAggregate
{
    [DynamoDBHashKey]
    [DynamoDBProperty("waiterId")]
    public required string WaiterId { get; set; }

    [DynamoDBProperty("locationId")]
    public required string LocationId { get; set; }

    [DynamoDBProperty("averageServiceRating")]
    public double AverageServiceRating { get; set; }

    [DynamoDBProperty("minimumServiceRating")]
    public int MinimumServiceRating { get; set; }

    [DynamoDBProperty("averageCuisineRating")]
    public double AverageCuisineRating { get; set; }

    [DynamoDBProperty("minimumCuisineRating")]
    public int MinimumCuisineRating { get; set; }

    [DynamoDBProperty("totalRevenue")]
    public decimal TotalRevenue { get; set; }

    [DynamoDBProperty("hoursWorked")]
    public double HoursWorked { get; set; }

    [DynamoDBRangeKey("date")]
    public DateTime Date { get; set; }

    [DynamoDBProperty("ordersProcessed")]
    public int OrdersProcessed { get; set; }
}