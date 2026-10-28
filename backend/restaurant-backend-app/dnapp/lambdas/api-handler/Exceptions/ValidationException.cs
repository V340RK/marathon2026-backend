using System;

namespace APIHandler.Exceptions;

public class ValidationException(string message) : Exception(message);