using Amazon.DynamoDBv2.DataModel;
using APIHandler.Constants;
using System;
using System.Collections.Generic;

namespace APIHandler.Models.WaiterList;

public class WaiterListItem
{
    [DynamoDBHashKey("userId")]
    public required string UserId { get; set; }

    [DynamoDBProperty("locationId")]
    [DynamoDBGlobalSecondaryIndexHashKey(GlobalSecondaryIndexes.Waiters.LocationId)]
    public required string LocationId { get; set; }

    [DynamoDBProperty("createdAt")]
    public DateTime CreatedAt { get; set; }

    [DynamoDBProperty("reservations")]
    public List<string> Reservations { get; set; } = [];
}
