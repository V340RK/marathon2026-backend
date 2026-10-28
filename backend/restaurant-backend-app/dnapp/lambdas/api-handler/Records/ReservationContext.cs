namespace APIHandler.Records;

public record ReservationContext(
    string? CustomerId,
    string? WaiterId,
    bool CreatedByWaiter
);
