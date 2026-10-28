using System;
using System.Text.Json.Serialization;

namespace APIHandler.Models.Notification;

public class NotificationResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("message")]
    public required string Message { get; set; }

    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }

    [JsonPropertyName("reservationId")]
    public string? ReservationId { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("isRead")]
    public bool IsRead { get; set; }
}