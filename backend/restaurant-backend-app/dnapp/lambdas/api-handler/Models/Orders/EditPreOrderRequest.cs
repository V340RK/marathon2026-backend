using APIHandler.Entities;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace APIHandler.Models.Orders;

public class EditPreOrderRequest
{
    [JsonPropertyName("orderItems")]
    public List<OrderItem> OrderItems { get; set; } = [];
}