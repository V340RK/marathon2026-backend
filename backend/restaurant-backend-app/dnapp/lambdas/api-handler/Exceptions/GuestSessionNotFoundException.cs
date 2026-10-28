namespace APIHandler.Exceptions;

public class GuestSessionNotFoundException(string message) : ValidationException(message);