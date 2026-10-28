using System.Text.Json.Serialization;

namespace APIHandler.Models.Users;

public class UploadProfileImageRequest
{
    [JsonPropertyName("base64Image")]
    public required string Base64Image { get; set; }
}