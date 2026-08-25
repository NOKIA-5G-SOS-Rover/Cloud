namespace backend.Constants;

public static class InjuryClasses
{
    public const string Unknown = "UNKNOWN";
    public const string None = "NONE";
    public const string Minor = "MINOR";
    public const string Moderate = "MODERATE";
    public const string Severe = "SEVERE";
    public const string Critical = "CRITICAL";

    public static readonly HashSet<string> Allowed =
        new(StringComparer.OrdinalIgnoreCase)
        {
            Unknown,
            None,
            Minor,
            Moderate,
            Severe,
            Critical
        };

    public static bool IsValid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
               && Allowed.Contains(value);
    }
}