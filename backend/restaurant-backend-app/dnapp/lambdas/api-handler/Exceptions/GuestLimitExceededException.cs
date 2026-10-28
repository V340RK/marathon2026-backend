namespace APIHandler.Exceptions;

public class GuestLimitExceededException(string message = "Guest limit exceeded for this reservation.") : ValidationException(message);