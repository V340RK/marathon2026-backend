using System.Text.Json.Serialization;

namespace APIHandler.Models.Dishes;

public class DishResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("dishType")]
    public required string DishType { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("weight")]
    public required string Weight { get; set; }

    [JsonPropertyName("calories")]
    public required string Calories { get; set; }

    [JsonPropertyName("proteins")]
    public required string Proteins { get; set; }

    [JsonPropertyName("fats")]
    public required string Fats { get; set; }

    [JsonPropertyName("carbohydrates")]
    public required string Carbohydrates { get; set; }

    [JsonPropertyName("vitamins")]
    public required string Vitamins { get; set; }

    [JsonPropertyName("imageUrl")]
    public required string? ImageUrl { get; set; }

    [JsonPropertyName("state")]
    public required string State { get; set; }
}