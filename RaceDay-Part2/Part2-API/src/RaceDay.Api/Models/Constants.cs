namespace RaceDay.Api.Models;


public static class AppRoles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";

    
    public static string? Normalise(string? role) => role?.Trim().ToLowerInvariant() switch
    {
        "organiser" => Organiser,
        "participant" => Participant,
        _ => null
    };
}


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
