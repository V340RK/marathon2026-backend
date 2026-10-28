using System.Text.Json.Serialization;

namespace APIHandler.Models.Reservations;

public class UpdateReservationTableRequest
{
    [JsonPropertyName("tableId")]
    public required string TableId { get; set; }
}
