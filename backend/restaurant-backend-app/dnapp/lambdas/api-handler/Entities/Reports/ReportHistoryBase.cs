using Amazon.DynamoDBv2.DataModel;
using APIHandler.Constants.Reports;
using System;
using System.Text.Json.Serialization;

namespace APIHandler.Entities.Reports;

public class ReportHistoryBase
{
    [DynamoDBHashKey("id")]
    [JsonIgnore]
    public required string Id { get; set; }

    [JsonPropertyName("reportType")]
    [DynamoDBProperty("reportType")]
    public ReportType ReportType { get; set; }

    [DynamoDBIgnore]
    [JsonIgnore]
    public DateOnly StartDate { get; set; }

    [DynamoDBIgnore]
    [JsonIgnore]
    public DateOnly EndDate { get; set; }

    [JsonPropertyName("startDate")]
    [DynamoDBProperty("startDate")]
    public string StartDateString
    {
        get => StartDate.ToString("yyyy-MM-dd");
        set => StartDate = DateOnly.Parse(value);
    }

    [JsonPropertyName("endDate")]
    [DynamoDBProperty("endDate")]
    public string EndDateString
    {
        get => EndDate.ToString("yyyy-MM-dd");
        set => EndDate = DateOnly.Parse(value);
    }

    [JsonPropertyName("userId")]
    [DynamoDBProperty("userId")]
    [DynamoDBGlobalSecondaryIndexHashKey("UserIdIndex")]
    public required string UserId { get; set; }

    [JsonPropertyName("locationId")]
    [DynamoDBProperty("locationId")]
    public required string LocationId { get; set; }

    [JsonPropertyName("locationName")]
    [DynamoDBProperty("locationName")]
    public required string LocationName { get; set; }
}