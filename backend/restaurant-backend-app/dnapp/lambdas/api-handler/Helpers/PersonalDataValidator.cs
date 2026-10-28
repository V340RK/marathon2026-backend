using APIHandler.Exceptions;
using System;
using System.Text.RegularExpressions;

namespace APIHandler.Helpers;

/// <summary>
/// A static class for validating personal data such as names, emails, and passwords.
/// This class provides methods to validate names, emails, and passwords according to specific rules.
/// </summary>
public static partial class PersonalDataValidator
{
    /// <summary>
    /// Validates a name to ensure it meets the specified criteria.
    /// The name must be between 2 and 50 characters long and can only contain Latin letters, hyphens, and apostrophes.
    /// </summary>
    /// <param name="name">The name to validate.</param>
    /// <param name="fieldName">The name of the field that is being validated.</param>
    /// <exception cref="ArgumentException">Thrown if the name is invalid.</exception>
    public static void ValidateName(string name, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 50)
        {
            throw new ValidationException($"{fieldName} must be between 2 and 50 characters long.");
        }

        if (!NameRegex().IsMatch(name))
        {
            throw new ValidationException($"{fieldName} can only contain Latin letters, hyphens, and apostrophes.");
        }
    }

    /// <summary>
    /// Validates an email address to ensure it meets the specified criteria.
    /// The email must be a valid format and cannot exceed 254 characters.
    /// </summary>
    /// <param name="email">The email to validate.</param>
    /// <exception cref="ArgumentException">Thrown if the email is invalid.</exception>
    public static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254)
        {
            throw new ValidationException("Email must be a valid email address and cannot exceed 254 characters.");
        }

        if (!EmailRegex().IsMatch(email))
        {
            throw new ValidationException("Invalid email address. Please ensure it follows the format: username@domain.com");
        }
    }

    /// <summary>
    /// Checks if the provided passwords meet the specified criteria.
    /// The password must be between 8 and 16 characters long, and the confirmation password must match the original password.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <param name="confirmPassword">Confirmation password. Must match the original password.</param>
    /// <exception cref="ArgumentException">Thrown if the password is invalid.</exception>
    public static void ValidatePasswords(string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8 || password.Length > 16)
        {
            throw new ValidationException("Password must be between 8 and 16 characters long.");
        }

        if (password != confirmPassword)
        {
            throw new ValidationException("Passwords do not match.");
        }
    }

    /// <summary>
    /// Validates that the provided password is not null, empty, or whitespace.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <exception cref="ArgumentException">Thrown if the password is null, empty, or consists only of whitespace characters.</exception>
    public static void ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException("Password is required. Please enter your password to continue.");
        }
    }

    [GeneratedRegex(@"^[a-zA-Z\-\']+$")]
    private static partial Regex NameRegex();

    [GeneratedRegex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$")]
    private static partial Regex EmailRegex();
}