using APIHandler.Constants.Dishes;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace APIHandler.Models.Dishes;

public class CreateDishRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; } 
        
    [JsonPropertyName("dishType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DishType DishType { get; set; }
        
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
        
    [JsonPropertyName("imageBase64")]
    public string? ImageBase64 { get; set; }
        
    [JsonPropertyName("state")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DishState State { get; set; } 
        
    [JsonPropertyName("locationIds")]
    public required List<string> LocationIds { get; set; }
}