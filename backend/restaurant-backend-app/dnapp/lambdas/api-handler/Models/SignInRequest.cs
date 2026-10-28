using System.Text.Json.Serialization;

namespace APIHandler.Models;

public class SignInRequest
{
    [JsonPropertyName("email")]
    public required string Email { get; set; }

    [JsonPropertyName("password")]
    public required string Password { get; set; }
}