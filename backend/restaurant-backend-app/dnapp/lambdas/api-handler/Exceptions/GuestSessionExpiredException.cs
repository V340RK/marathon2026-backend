namespace APIHandler.Exceptions;

public class GuestSessionExpiredException(string message) : ValidationException(message)
{
}
