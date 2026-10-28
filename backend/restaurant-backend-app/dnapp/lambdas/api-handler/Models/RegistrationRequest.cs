using System.Text.Json.Serialization;

namespace APIHandler.Models;

/// <summary>
/// Represents a request for user registration.
/// </summary>
public class RegistrationRequest
{
    /// <summary>
    /// Gets or sets the first name of the user.
    /// The first name must be between 2 and 50 characters long and can only contain Latin letters, hyphens, and apostrophes.
    /// </summary>
    [JsonPropertyName("firstName")]
    public required string FirstName { get; set; }

    /// <summary>
    /// Gets or sets the last name of the user.
    /// The last name must be between 2 and 50 characters long and can only contain Latin letters, hyphens, and apostrophes.
    /// </summary>
    [JsonPropertyName("lastName")]
    public required string LastName { get; set; }

    /// <summary>
    /// Gets or sets the email address of the user.
    /// The email must be a valid format and cannot exceed 254 characters.
    /// </summary>
    [JsonPropertyName("email")]
    public required string Email { get; set; }

    /// <summary>
    /// Gets or sets the password for the user.
    /// The password must be between 8 and 16 characters long, and must contain
    /// at least one uppercase letter, one lowercase letter, one digit, and one
    /// special character.
    /// </summary>
    [JsonPropertyName("password")]
    public required string Password { get; set; }

    /// <summary>
    /// Gets or sets the confirmation password for the user.
    /// The confirmation password must match the original password.
    /// </summary>
    [JsonPropertyName("confirmPassword")]
    public required string ConfirmPassword { get; set; }
}