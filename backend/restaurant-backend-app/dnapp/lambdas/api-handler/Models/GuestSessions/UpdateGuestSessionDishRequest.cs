using System.Text.Json.Serialization;

namespace APIHandler.Models.GuestSessions;

public class UpdateGuestSessionDishRequest
{
    [JsonPropertyName("dishId")]
    public string DishId { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}