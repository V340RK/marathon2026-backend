using System;
using System.Text.Json.Serialization;

namespace APIHandler.Models.Feedbacks;

public class FeedbackResponseItem
{
    [JsonPropertyName("reservationId")]
    public required string ReservationId { get; set; }

    [JsonPropertyName("locationId")]
    public required string LocationId { get; set; }

    [JsonPropertyName("userId")]
    public string? UserId { get; set; } // nullable for future implementation of anonymous

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [JsonPropertyName("rating")]
    public int Rating { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("type")]
    public FeedbackType Type { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; } // optional, can be null if anonymous
}