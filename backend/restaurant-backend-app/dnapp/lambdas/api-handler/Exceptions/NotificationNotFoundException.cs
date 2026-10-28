using System;

namespace APIHandler.Exceptions;

public class NotificationNotFoundException() : ValidationException("Notification not found");