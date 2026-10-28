using System.Text.Json.Serialization;

namespace APIHandler.Models.Users;

public class ChangeUserPasswordRequest
{
    [JsonPropertyName("oldPassword")]
    public string OldPassword { get; set; } = string.Empty;

    [JsonPropertyName("newPassword")]
    public string NewPassword { get; set; } = string.Empty;

    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;
}