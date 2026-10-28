using System.Text.Json.Serialization;

namespace ReportSender.Entities.Reports;

public class ReportHistoryLocation : ReportHistoryBase
{
    [JsonPropertyName("totalOrders")]
    public int TotalOrders { get; set; }

    [JsonPropertyName("avgCuisineFeedback")]
    public double AvgCuisineFeedback { get; set; }

    [JsonPropertyName("minCuisineFeedback")]
    public double MinCuisineFeedback { get; set; }

    [JsonPropertyName("revenue")]
    public double Revenue { get; set; }

    [JsonPropertyName("deltaAvgCuisineFeedback")]
    public double DeltaAvgCuisineFeedback { get; set; }

    [JsonPropertyName("deltaRevenue")]
    public double DeltaRevenue { get; set; }
}
