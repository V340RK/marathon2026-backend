using System.Text.Json.Serialization;

namespace APIHandler.Models.Locations;

public class CreateLocationRequest
{
    [JsonPropertyName("address")]
    public required string Address { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("totalCapacity")]
    public int TotalCapacity { get; set; }

    [JsonPropertyName("averageOccupancy")]
    public required string AverageOccupancy { get; set; }

    [JsonPropertyName("imageBase64")]
    public string? ImageBase64 { get; set; }

    [JsonPropertyName("rating")]
    public double Rating { get; set; }
}