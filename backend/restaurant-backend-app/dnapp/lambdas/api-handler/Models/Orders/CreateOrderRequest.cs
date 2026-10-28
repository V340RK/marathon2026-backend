using APIHandler.Entities;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace APIHandler.Models.Orders;

public class CreateOrderRequest
{
    [JsonPropertyName("reservationId")]
    public required string ReservationId { get; set; }

    [JsonPropertyName("orderItems")]
    public List<OrderItem> OrderItems { get; set; } = [];
}