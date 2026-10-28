using APIHandler.Constants.Orders;
using System.Text.Json.Serialization;

namespace APIHandler.Models.GuestSessions;

public class GuestSessionDishResponse
{
    [JsonPropertyName("dishId")]
    public string DishId { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("state")]
    public OrderItemState State { get; set; }

    [JsonPropertyName("price")]
    public double Price { get; set; }
}
