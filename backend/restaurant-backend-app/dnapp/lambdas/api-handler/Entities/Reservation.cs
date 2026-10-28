using Amazon.DynamoDBv2.DataModel;
using APIHandler.Constants;
using System;
using System.Text.Json.Serialization;

namespace APIHandler.Entities;

[DynamoDBTable("Reservations")]
public class Reservation
{
    [DynamoDBHashKey("id")]
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [DynamoDBProperty("tableId")]
    [DynamoDBGlobalSecondaryIndexHashKey("GSI_ByTableId")]
    [JsonPropertyName("tableId")]
    public required string TableId { get; set; }

    [DynamoDBProperty("customerId")]
    [DynamoDBGlobalSecondaryIndexHashKey("GSI_UserId")]
    [JsonPropertyName("customerId")]
    public string? CustomerId { get; set; }

    [DynamoDBProperty("waiterId")]
    [DynamoDBGlobalSecondaryIndexHashKey("GSI_WaiterId")]
    [JsonPropertyName("waiterId")]
    public required string WaiterId { get; set; }

    [DynamoDBProperty("createdByWaiter")]
    [JsonPropertyName("createdByWaiter")]
    public bool CreatedByWaiter { get; set; }

    [DynamoDBProperty("startTime")]
    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; }

    [DynamoDBProperty("endTime")]
    [JsonPropertyName("endTime")]
    public DateTime EndTime { get; set; }

    [DynamoDBProperty("guestCount")]
    [JsonPropertyName("guestCount")]
    public int GuestCount { get; set; }

    [DynamoDBProperty("status")]
    [JsonPropertyName("status")]
    public ReservationStatus Status { get; set; }
}