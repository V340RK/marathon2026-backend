using Amazon.DynamoDBv2.DataModel;
using APIHandler.Constants;
using System.Collections.Generic;

namespace APIHandler.Entities;

public class GuestSession
{
    [DynamoDBHashKey("guestSessionId")]
    public required string Id { get; set; }

    [DynamoDBProperty("groupReservationId")]
    [DynamoDBGlobalSecondaryIndexHashKey(GlobalSecondaryIndexes.GuestSession.GroupReservationId)]
    public string GroupReservationId { get; set; } = string.Empty;

    [DynamoDBProperty("guestName")]
    public required string GuestName { get; set; }

    [DynamoDBProperty("dishes")]
    public required List<OrderItem> Dishes { get; set; }

    [DynamoDBProperty("guestSessionTokenHash")]
    [DynamoDBGlobalSecondaryIndexHashKey(GlobalSecondaryIndexes.GuestSession.SessionTokenHash)]
    public string? SessionTokenHash { get; set; }

    [DynamoDBProperty("isOwnerSession")]
    public bool IsOwnerSession { get; set; }

    [DynamoDBProperty("ownerUserId")]
    public string? OwnerUserId { get; set; }

    [DynamoDBProperty("isSubmitted")]
    public bool IsSubmitted { get; set; }

    [DynamoDBProperty("billType")]
    public BillType BillType { get; set; } = BillType.Individual;
}