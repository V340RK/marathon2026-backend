namespace APIHandler.Exceptions;

public class DishIsNotAvailableException(string message = "Dish is not available.") : ValidationException(message);