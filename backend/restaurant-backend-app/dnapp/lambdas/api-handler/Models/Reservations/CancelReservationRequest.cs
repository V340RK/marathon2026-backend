using System.Text.Json.Serialization;

namespace APIHandler.Models.Reservations;

public class CancelReservationRequest
{
    [JsonPropertyName("id")]
    public string ReservationId { get; set; } = default!;
}
