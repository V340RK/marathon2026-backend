using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace APIHandler.Models.Feedbacks;

public class FeedbackResponse
{
    [JsonPropertyName("items")]
    public List<FeedbackResponseItem> Items { get; set; } = [];

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("currentPage")]
    public int CurrentPage { get; set; }
}