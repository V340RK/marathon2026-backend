using Amazon.DynamoDBv2.DataModel;
using APIHandler.Constants;
using System.Text.Json.Serialization;

namespace APIHandler.Entities;

[DynamoDBTable("Tables")]
public class Table
{
    [DynamoDBHashKey("id")]
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [DynamoDBGlobalSecondaryIndexHashKey(GlobalSecondaryIndexes.Tables.LocationIdTableNumber)]
    [DynamoDBProperty("locationId")]
    [JsonPropertyName("locationId")]
    public required string LocationId { get; set; }

    [DynamoDBProperty("capacity")]
    [JsonPropertyName("capacity")]
    public int Capacity { get; set; }

    [DynamoDBGlobalSecondaryIndexRangeKey(GlobalSecondaryIndexes.Tables.LocationIdTableNumber)]
    [DynamoDBProperty("tableNumber")]
    [JsonPropertyName("tableNumber")]
    public int TableNumber { get; set; }
}
