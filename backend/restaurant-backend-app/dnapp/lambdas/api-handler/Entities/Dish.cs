using Amazon.DynamoDBv2.DataModel;
using APIHandler.Constants.Dishes;
using APIHandler.Converters;

namespace APIHandler.Entities;

[DynamoDBTable("Dishes")]
public class Dish
{
    [DynamoDBHashKey("id")]
    public required string Id { get; set; }

    [DynamoDBProperty("name")]
    public string Name { get; set; } = default!;

    [DynamoDBProperty("description")]
    public string Description { get; set; } = default!;

    [DynamoDBProperty("dishType", typeof(EnumAsIntConverter<DishType>))]
    public DishType DishType { get; set; }

    [DynamoDBProperty("price")]
    public decimal Price { get; set; }

    [DynamoDBProperty("weight")]
    public string Weight { get; set; } = default!;

    [DynamoDBProperty("calories")]
    public string Calories { get; set; } = default!;

    [DynamoDBProperty("proteins")]
    public string Proteins { get; set; } = default!;

    [DynamoDBProperty("fats")]
    public string Fats { get; set; } = default!;

    [DynamoDBProperty("carbohydrates")]
    public string Carbohydrates { get; set; } = default!;

    [DynamoDBProperty("vitamins")]
    public string Vitamins { get; set; } = default!;

    [DynamoDBProperty("imageUrl")]
    public string? ImageUrl { get; set; }

    [DynamoDBProperty("state", typeof(EnumAsIntConverter<DishState>))]
    public DishState State { get; set; }

    [DynamoDBProperty("locationId")]
    public string LocationId { get; set; }
}
