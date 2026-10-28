using System.Text.Json.Serialization;

namespace APIHandler.Models.WaiterList;

public class UpdateWaiterListRequest
{
    [JsonPropertyName("email")]
    public required string Email { get; set; }

    [JsonPropertyName("locationId")]
    public required string LocationId { get; set; }
}