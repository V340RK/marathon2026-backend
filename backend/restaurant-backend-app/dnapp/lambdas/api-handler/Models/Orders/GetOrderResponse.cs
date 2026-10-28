using APIHandler.Entities;
using APIHandler.Models.Dishes;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace APIHandler.Models.Orders;

public class GetOrderResponse
{
    [JsonPropertyName("orderId")]
    public required string OrderId { get; set; }

    [JsonPropertyName("reservationId")]
    public required string ReservationId { get; set; }

    [JsonPropertyName("reservationAddress")]
    public required string ReservationAddress { get; set; }

    [JsonPropertyName("reservationTime")]
    public required string ReservationTime { get; set; }

    [JsonPropertyName("orderItems")]
    public required List<OrderItemResponse> OrderItems { get; set; }

    [JsonPropertyName("status")]
    public OrderStatus Status { get; set; }
}