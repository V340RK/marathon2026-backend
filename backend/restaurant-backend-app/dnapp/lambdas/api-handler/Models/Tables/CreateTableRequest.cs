using System.Text.Json.Serialization;

namespace APIHandler.Models.Tables;

public class CreateTableRequest
{
    [JsonPropertyName("locationId")]
    public required string LocationId { get; set; }

    [JsonPropertyName("capacity")]
    public int Capacity { get; set; }

    [JsonPropertyName("tableNumber")]
    public int TableNumber { get; set; }
}