using Amazon.DynamoDBv2.DataModel;
using System;
using System.Text.Json.Serialization;

namespace APIHandler.Entities;

[DynamoDBTable("Notifications")]
public class Notification
{
    [DynamoDBHashKey("id")]
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [DynamoDBProperty("userId")]
    [DynamoDBGlobalSecondaryIndexHashKey("UserIdIndex")]
    [JsonPropertyName("userId")]
    public required string UserId { get; set; }

    [DynamoDBProperty("message")]
    [JsonPropertyName("message")]
    public required string Message { get; set; }

    [DynamoDBProperty("orderId")]
    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }

    [DynamoDBProperty("reservationId")]
    [JsonPropertyName("reservationId")]
    public string? ReservationId { get; set; }

    [DynamoDBProperty("isRead")]
    [JsonPropertyName("isRead")]
    public bool IsRead { get; set; } = false;

    [DynamoDBProperty("createdAt")]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}