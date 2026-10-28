using APIHandler.Entities;
using APIHandler.Models.Orders;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace APIHandler.Models.GuestSessions;

public record GetAllSessionsResponse(
    IEnumerable<Participant> Participants,
    decimal Total,
    decimal SharedTotal = 0,
    decimal IndividualTotal = 0);

public record Participant(
    string? Id,
    [property: JsonConverter(typeof(JsonStringEnumConverter<ParticipantType>))] ParticipantType Type,
    string Name,
    IEnumerable<OrderItemResponse> Items,
    decimal Total,
    bool IsSubmitted,
    BillType BillType = BillType.Individual);

public enum ParticipantType
{
    Owner,
    Guest
}
