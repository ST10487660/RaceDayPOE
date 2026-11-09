namespace RaceDay.Web.Infrastructure;

public static class ViewHelpers
{
    /// <summary>"Today" for South African users (UTC+2).</summary>
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(2));

    public static string Initials(string? name)
    {
        var parts = (name ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1].ToUpperInvariant(),
            _ => (parts[0][..1] + parts[^1][..1]).ToUpperInvariant()
        };
    }

    public static string TypeIcon(string? eventType) => eventType switch
    {
        "Run" => "\U0001F3C3",
        "Walk" => "\U0001F6B6",
        "Cycle" => "\U0001F6B4",
        _ => "\U0001F3C1"
    };

    public static string FormatTime(TimeSpan? time) => time?.ToString(@"hh\:mm\:ss") ?? "-";

    public static string Ordinal(int n)
    {
        var suffix = (n % 100) is >= 11 and <= 13 ? "th" : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
        return n + suffix;
    }
}
