using Amazon.DynamoDBv2.DataModel;
using System.Text.Json.Serialization;

namespace APIHandler.Entities.Reports;

[DynamoDBTable("ReportsHistory")]
public class ReportHistoryWaiter : ReportHistoryBase
{
    [JsonPropertyName("waiterId")]
    [DynamoDBProperty("waiterId")]
    public required string WaiterId { get; set; }

    [JsonPropertyName("waiterName")]
    [DynamoDBProperty("waiterName")]
    public required string WaiterName { get; set; }

    [JsonPropertyName("waiterEmail")]
    [DynamoDBProperty("waiterEmail")]
    public required string WaiterEmail { get; set; }

    [JsonPropertyName("workingHours")]
    [DynamoDBProperty("workingHours")]
    public double WorkingHours { get; set; }

    [JsonPropertyName("totalOrders")]
    [DynamoDBProperty("totalOrders")]
    public int TotalOrders { get; set; }

    [JsonPropertyName("avgServiceFeedback")]
    [DynamoDBProperty("avgServiceFeedback")]
    public double AvgServiceFeedback { get; set; }

    [JsonPropertyName("minServiceFeedback")]
    [DynamoDBProperty("minServiceFeedback")]
    public double MinServiceFeedback { get; set; }

    [JsonPropertyName("deltaAvgServiceFeedback")]
    [DynamoDBProperty("deltaAvgServiceFeedback")]
    public double DeltaAvgServiceFeedback { get; set; }

    [JsonPropertyName("deltaTotalOrders")]
    [DynamoDBProperty("deltaTotalOrders")]
    public double DeltaTotalOrders { get; set; }
}