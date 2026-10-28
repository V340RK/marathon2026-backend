using System.ComponentModel.DataAnnotations;

namespace APIHandler.Models.GuestSessions;

public record CreateGuestSessionRequest(
    [property: Required] string GroupReservationId, 
    [property: Required] string GuestName);