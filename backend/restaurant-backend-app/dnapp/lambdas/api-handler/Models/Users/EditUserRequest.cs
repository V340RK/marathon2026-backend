using System.Text.Json.Serialization;

namespace APIHandler.Models.Users;

public class EditUserRequest
{
    [JsonPropertyName("firstName")]
    public required string FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public required string LastName { get; set; }
}