using Amazon.DynamoDBv2.DataModel;
using APIHandler.Converters;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace APIHandler.Entities;

public class Order
{
    [DynamoDBHashKey("orderId")]
    [JsonPropertyName("orderId")]
    public required string OrderId { get; set; }

    [DynamoDBGlobalSecondaryIndexHashKey("GSI_CustomerId")]
    [DynamoDBRangeKey("customerId")]
    [JsonPropertyName("customerId")]
    public required string CustomerId { get; set; }

    [DynamoDBGlobalSecondaryIndexHashKey("GSI_ReservationId")]
    [DynamoDBProperty("reservationId")]
    [JsonPropertyName("reservationId")]
    public required string ReservationId { get; set; }

    [DynamoDBProperty("reservationAddress")]
    [JsonPropertyName("reservationAddress")]
    public required string ReservationAddress { get; set; } 
    //  to avoid joining with Reservation table. example "some street 19, Table 1"

    [DynamoDBProperty("reservationTime")]
    [JsonPropertyName("reservationTime")]
    public required string ReservationTime { get; set; }
    //  to avoid joining with Reservation table. example "Oct 14, 2025, 9:00 a.m. - 10:00 a.m."

    [DynamoDBProperty("dishes")]
    [JsonPropertyName("dishes")]
    public required List<OrderItem> Dishes { get; set; }

    [DynamoDBProperty("status", typeof(EnumAsIntConverter<OrderStatus>))]
    [JsonPropertyName("status")]
    public OrderStatus Status { get; set; }
}