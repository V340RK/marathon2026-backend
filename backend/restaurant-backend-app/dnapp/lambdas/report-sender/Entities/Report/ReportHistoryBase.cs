using System;
using System.Text.Json.Serialization;

namespace ReportSender.Entities.Reports;

public abstract class ReportHistoryBase
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("reportType")]
    public ReportType ReportType { get; set; }

    [JsonIgnore]
    public DateOnly StartDate { get; set; }

    [JsonIgnore]
    public DateOnly EndDate { get; set; }

    [JsonPropertyName("startDate")]
    public string StartDateString
    {
        get => StartDate.ToString("yyyy-MM-dd");
        set => StartDate = DateOnly.Parse(value);
    }

    [JsonPropertyName("endDate")]
    public string EndDateString
    {
        get => EndDate.ToString("yyyy-MM-dd");
        set => EndDate = DateOnly.Parse(value);
    }

    [JsonPropertyName("userId")]
    public string UserId { get; set; }

    [JsonPropertyName("locationId")]
    public string LocationId { get; set; }

    [JsonPropertyName("locationName")]
    public string LocationName { get; set; }
}
