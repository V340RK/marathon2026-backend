using System;

namespace APIHandler.Exceptions;

public class ActionForbiddenException(string message) : Exception(message);