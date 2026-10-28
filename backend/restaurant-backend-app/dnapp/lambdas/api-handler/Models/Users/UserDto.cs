using System.Text.Json.Serialization;

namespace APIHandler.Models.Users;

public class UserDto
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("firstName")]
    public required string FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public required string LastName { get; set; }

    [JsonPropertyName("email")]
    public required string Email { get; set; }

    [JsonPropertyName("role")]
    public required string Role { get; set; }
}