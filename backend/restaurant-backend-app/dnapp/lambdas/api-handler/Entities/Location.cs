using Amazon.DynamoDBv2.DataModel;
using System.Collections.Generic;

namespace APIHandler.Entities;

[DynamoDBTable("Locations")]
public class Location
{
    [DynamoDBHashKey("id")]
    public required string Id { get; set; }

    [DynamoDBProperty("address")]
    public required string Address { get; set; }

    [DynamoDBProperty("description")]
    public required string Description { get; set; }

    [DynamoDBProperty("totalCapacity")]
    public int TotalCapacity { get; set; }

    [DynamoDBProperty("averageOccupancy")]
    public required string AverageOccupancy { get; set; }

    [DynamoDBProperty("imageUrl")]
    public string? ImageUrl { get; set; }

    [DynamoDBProperty("rating")]
    public double Rating { get; set; }

    [DynamoDBProperty("specialtyDishIds")]
    public List<string>? SpecialtyDishIds { get; set; }
}
