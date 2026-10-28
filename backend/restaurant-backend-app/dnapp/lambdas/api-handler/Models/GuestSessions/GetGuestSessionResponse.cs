using APIHandler.Entities;
using APIHandler.Models.Orders;
using System.Collections.Generic;

namespace APIHandler.Models.GuestSessions;

public record GetGuestSessionResponse(
    string Name,
    IEnumerable<OrderItemResponse> Items,
    decimal Total,
    bool IsSubmitted,
    BillType BillType = BillType.Individual);
