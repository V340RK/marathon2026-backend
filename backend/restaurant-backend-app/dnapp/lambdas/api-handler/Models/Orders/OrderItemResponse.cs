using APIHandler.Models.Dishes;
using System.Text.Json.Serialization;

namespace APIHandler.Models.Orders;

public class OrderItemResponse
{
    [JsonPropertyName("dish")]
    public required DishResponse Dish { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}
