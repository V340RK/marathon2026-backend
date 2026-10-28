using System;
using System.Text.Json.Serialization;

namespace APIHandler.Models.Reservations;

public class CreateReservationRequest
{
    [JsonPropertyName("tableId")]
    public required string TableId { get; set; }

    /// Optional – For waiter use only. Email of existing customer in Cognito.
    [JsonPropertyName("customerEmail")]
    public string? CustomerEmail { get; set; }

    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public DateTime EndTime { get; set; }

    [JsonPropertyName("guestCount")]
    public int GuestCount { get; set; }

    [JsonPropertyName("customerId")]
    public string? CustomerId { get; set; }
}