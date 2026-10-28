using System;

namespace APIHandler.Helpers;

public static class EqualityHelper
{
    public static bool EqualsIgnoreCase(string? value1, string? value2)
        => string.Equals(value1, value2, StringComparison.OrdinalIgnoreCase);

    public static bool NotEqualsIgnoreCase(string? value1, string? value2)
        => !EqualsIgnoreCase(value1, value2);

    public static bool EqualsCaseSensitive(string? value1, string? value2)
        => string.Equals(value1, value2, StringComparison.Ordinal);

    public static bool NotEqualsCaseSensitive(string? value1, string? value2)
        => !EqualsCaseSensitive(value1, value2);
}
