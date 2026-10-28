using System;
using System.Linq;

namespace APIHandler.Helpers;

public static class OccupancyHelper
{
    /// <summary>
    /// Strict validation for POST/PUT. Only allows formats like "65" or "65%".
    /// </summary>
    public static bool TryParseStrict(string? input, out string formattedOccupancy)
    {
        formattedOccupancy = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        var trimmed = input.Trim();
        if (trimmed.EndsWith('%'))
        {
            trimmed = trimmed[..^1];
        }

        if (!trimmed.All(char.IsDigit))
            return false;

        if (int.TryParse(trimmed, out int value) && value >= 0 && value <= 100)
        {
            formattedOccupancy = $"{value}%";
            return true;
        }

        return false;
    }

    /// <summary>
    /// Lenient sanitization for GET requests to clean up legacy data like "High".
    /// </summary>
    public static string SanitizeLegacy(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "0%";

        var digitsOnly = new string(input.Where(char.IsDigit).ToArray());

        if (int.TryParse(digitsOnly, out int value))
        {
            return $"{Math.Clamp(value, 0, 100)}%";
        }

        // Fallback for legacy strings that had no numbers
        return input.Trim().ToLowerInvariant() switch
        {
            "high" => "75%",
            "medium" => "50%",
            "low" => "25%",
            _ => "0%"
        };
    }
}