using Amazon.DynamoDBv2.DataModel;
using System.Text.Json.Serialization;

namespace APIHandler.Entities.Reports;

[DynamoDBTable("ReportsHistory")]
public class ReportHistoryLocation : ReportHistoryBase
{
    [JsonPropertyName("totalOrders")]
    [DynamoDBProperty("totalOrders")]
    public int TotalOrders { get; set; }

    [JsonPropertyName("avgCuisineFeedback")]
    [DynamoDBProperty("avgCuisineFeedback")]
    public double AvgCuisineFeedback { get; set; }

    [JsonPropertyName("minCuisineFeedback")]
    [DynamoDBProperty("minCuisineFeedback")]
    public double MinCuisineFeedback { get; set; }

    [JsonPropertyName("revenue")]
    [DynamoDBProperty("revenue")]
    public decimal Revenue { get; set; }

    [JsonPropertyName("deltaAvgCuisineFeedback")]
    [DynamoDBProperty("deltaAvgCuisineFeedback")]
    public double DeltaAvgCuisineFeedback { get; set; }

    [JsonPropertyName("deltaRevenue")]
    [DynamoDBProperty("deltaRevenue")]
    public decimal DeltaRevenue { get; set; }
}