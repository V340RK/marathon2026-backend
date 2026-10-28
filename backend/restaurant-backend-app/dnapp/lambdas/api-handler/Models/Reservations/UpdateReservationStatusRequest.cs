using APIHandler.Constants;
using System.Text.Json.Serialization;

namespace APIHandler.Models.Reservations;

public class UpdateReservationStatusRequest
{
    [JsonPropertyName("newStatus")]
    public ReservationStatus NewStatus { get; set; }
}