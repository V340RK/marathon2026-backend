using System.Text.Json.Serialization;

namespace APIHandler.Models.Users;

public class ProfileImageResponse
{
    [JsonPropertyName("presignedUrl")]
    public required string PresignedUrl { get; set; }
}