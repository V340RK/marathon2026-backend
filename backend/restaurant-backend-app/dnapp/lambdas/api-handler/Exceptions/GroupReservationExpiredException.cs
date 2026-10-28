namespace APIHandler.Exceptions;

public class GroupReservationExpiredException(string message = "Group reservation is expired.") : ValidationException(message);