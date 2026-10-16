namespace RaceDay.Api.Models;

/// <summary>The two roles in RaceDay.</summary>
public static class AppRoles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";

    /// <summary>Returns the canonical role name, or null if the value is not a valid role.</summary>
    public static string? Normalise(string? role) => role?.Trim().ToLowerInvariant() switch
    {
        "organiser" => Organiser,
        "participant" => Participant,
        _ => null
    };
}

/// <summary>Allowed event types (run, walk or cycle).</summary>
public static class EventTypes
{
    public const string Run = "Run";
    public const string Walk = "Walk";
    public const string Cycle = "Cycle";

    public static string? Normalise(string? type) => type?.Trim().ToLowerInvariant() switch
    {
        "run" => Run,
        "walk" => Walk,
        "cycle" => Cycle,
        _ => null
    };
}

public static class EnrolmentStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
}
