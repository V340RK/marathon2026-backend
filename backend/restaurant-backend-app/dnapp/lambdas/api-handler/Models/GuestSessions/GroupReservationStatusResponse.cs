using APIHandler.Entities;
using System.Text.Json.Serialization;

namespace APIHandler.Models.GuestSessions;

public class GroupReservationStatusResponse
{
    [JsonPropertyName("currentParticipants")]
    public int CurrentParticipants { get; set; }

    [JsonPropertyName("maxCapacity")]
    public int MaxCapacity { get; set; }

    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter<GroupReservationStatus>))]
    public GroupReservationStatus Status { get; set; }
}
