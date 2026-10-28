using Amazon.DynamoDBv2.DataModel;
using APIHandler.Constants;
using APIHandler.Converters;
using System;

namespace APIHandler.Models.Feedbacks;

[DynamoDBTable("Feedback")]
public class Feedback
{
    [DynamoDBHashKey]
    [DynamoDBProperty("reservationId")]
    public required string ReservationId { get; set; }

    [DynamoDBGlobalSecondaryIndexHashKey(GlobalSecondaryIndexes.Feedback.LocationIdCreatedAt)]
    [DynamoDBProperty("locationId")]
    public required string LocationId { get; set; }

    [DynamoDBProperty("userId")]
    public string? UserId { get; set; } // nullable for future implementation of anonymous feedback

    [DynamoDBProperty("comment")]
    public string? Comment { get; set; }

    [DynamoDBProperty("rating")]
    public int Rating { get; set; }

    [DynamoDBRangeKey]
    [DynamoDBGlobalSecondaryIndexRangeKey(GlobalSecondaryIndexes.Feedback.LocationIdCreatedAt)]
    [DynamoDBProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [DynamoDBProperty("type", typeof(EnumAsIntConverter<FeedbackType>))]
    public FeedbackType Type { get; set; }
}
