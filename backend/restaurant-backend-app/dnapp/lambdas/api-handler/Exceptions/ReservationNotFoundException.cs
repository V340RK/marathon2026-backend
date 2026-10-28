using System;

namespace APIHandler.Exceptions;

public class ReservationNotFoundException(string message) : ValidationException("Reservation not found.");