namespace APIHandler.Constants;

public static class GlobalSecondaryIndexes
{
    public static class Tables
    {
        public const string LocationIdTableNumber = "locationId-tableNumber-index";
    }

    public static class Feedback
    {
        public const string LocationIdCreatedAt = "locationId-createdAt-index";
    }

    public static class Waiters
    {
        public const string LocationId = "locationId-index";
    }

    public static class GuestSession
    {
        public const string GroupReservationId = "groupReservationId";
        public const string SessionTokenHash = "guestSessionTokenHash";
    }

    public static class GroupReservation
    {
        public const string ReservationToken = "reservationToken";

        public const string ReservationId = "reservationId";
    }
}
