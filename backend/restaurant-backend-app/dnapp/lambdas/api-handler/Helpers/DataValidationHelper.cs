namespace APIHandler.Helpers;

public static class DataValidationHelper
{
    public static bool IsMissing(string? value)
        => string.IsNullOrWhiteSpace(value);
}
