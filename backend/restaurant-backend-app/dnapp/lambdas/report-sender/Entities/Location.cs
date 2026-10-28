using Amazon.DynamoDBv2.DataModel;
using System.Collections.Generic;

namespace ReportSender.Entities.Locations;

[DynamoDBTable("Locations")]
public class Location
{
    [DynamoDBHashKey("id")]
    public string Id { get; set; }

    [DynamoDBProperty("address")]
    public string Address { get; set; }

    [DynamoDBProperty("description")]
    public string Description { get; set; }

    [DynamoDBProperty("totalCapacity")]
    public int TotalCapacity { get; set; }

    [DynamoDBProperty("averageOccupancy")]
    public string AverageOccupancy { get; set; }

    [DynamoDBProperty("imageUrl")]
    public string ImageUrl { get; set; }

    [DynamoDBProperty("rating")]
    public double Rating { get; set; }

    [DynamoDBProperty("specialtyDishIds")]
    public List<string>? SpecialtyDishIds { get; set; }
}
