using System.Text.Json.Serialization;

namespace APIHandler.Models.Locations;

public class GetLocationResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("totalCapacity")]
    public int TotalCapacity { get; set; }

    [JsonPropertyName("averageOccupancy")]
    public string AverageOccupancy { get; set; } = string.Empty;

    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; } = string.Empty;

    [JsonPropertyName("rating")]
    public double Rating { get; set; }
}
