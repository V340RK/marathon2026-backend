using System.Text.Json.Serialization;

namespace ReportSender.Entities.Reports;

public class ReportHistoryWaiter : ReportHistoryBase
{
    [JsonPropertyName("waiterId")]
    public string WaiterId { get; set; }

    [JsonPropertyName("waiterName")]
    public string WaiterName { get; set; }

    [JsonPropertyName("waiterEmail")]
    public string WaiterEmail { get; set; }

    [JsonPropertyName("workingHours")]
    public double WorkingHours { get; set; }

    [JsonPropertyName("totalOrders")]
    public int TotalOrders { get; set; }

    [JsonPropertyName("avgServiceFeedback")]
    public double AvgServiceFeedback { get; set; }

    [JsonPropertyName("minServiceFeedback")]
    public double MinServiceFeedback { get; set; }

    [JsonPropertyName("deltaAvgServiceFeedback")]
    public double DeltaAvgServiceFeedback { get; set; }

    [JsonPropertyName("deltaTotalOrders")]
    public double DeltaTotalOrders { get; set; }
}
