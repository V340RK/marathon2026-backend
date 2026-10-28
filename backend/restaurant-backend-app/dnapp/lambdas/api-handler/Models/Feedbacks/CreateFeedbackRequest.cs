using System.Text.Json.Serialization;

namespace APIHandler.Models.Feedbacks;

public class CreateFeedbackRequest
{
    [JsonPropertyName("reservationId")]
    public required string ReservationId { get; set; }

    [JsonPropertyName("userId")]
    public string? UserId { get; set; } // nullable for future implementation of anonymous feedback

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [JsonPropertyName("rating")]
    public int Rating { get; set; }

    [JsonPropertyName("type")]
    public FeedbackType Type { get; set; }
}