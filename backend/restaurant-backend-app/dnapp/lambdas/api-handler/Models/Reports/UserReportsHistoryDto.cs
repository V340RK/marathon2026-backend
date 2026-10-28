using APIHandler.Constants.Reports;
using System;
using System.Text.Json.Serialization;

namespace APIHandler.Models.Reports;

public class UserReportsHistoryDto
{
    [JsonPropertyName("startDate")]
    public DateOnly StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public DateOnly EndDate { get; set; }

    [JsonPropertyName("userId")]
    public required string UserId { get; set; }
    
    [JsonPropertyName("locationId")]
    public required string LocationId { get; set; }
    
    [JsonPropertyName("locationName")]
    public string LocationName { get; set; } = "Unknown";

    [JsonPropertyName("reportType")]
    public ReportType ReportType { get; set; }
}
