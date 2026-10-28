using System;

namespace APIHandler.Exceptions;

public class DishNotFoundException(string message) : ValidationException(message);
