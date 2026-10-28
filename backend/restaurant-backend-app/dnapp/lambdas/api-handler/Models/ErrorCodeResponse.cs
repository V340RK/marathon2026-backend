using System.Text.Json.Serialization;

namespace APIHandler.Models;

[JsonConverter(typeof(JsonStringEnumConverter<ErrorCode>))]
public enum ErrorCode
{
    [JsonStringEnumMemberName("GroupReservationExpired")]
    GroupReservationExpired,

    [JsonStringEnumMemberName("GroupReservationSubmitted")]
    GroupReservationSubmitted,

    [JsonStringEnumMemberName("GuestLimitExceeded")]
    GuestLimitExceeded,

    [JsonStringEnumMemberName("GuestSessionExpired")]
    GuestSessionExpired,

    [JsonStringEnumMemberName("Unknown")]
    Unknown,
}

public record ErrorCodeResponse(string Message, ErrorCode ErrorCode);