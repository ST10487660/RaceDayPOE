namespace RaceDay.Api.Models;

public class Event
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public User Organiser { get; set; } = null!;
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    /// <summary>Run, Walk or Cycle.</summary>
    public string EventType { get; set; } = EventTypes.Run;
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();
    public EventRoute? Route { get; set; }
    public ICollection<WeatherRecord> WeatherRecords { get; set; } = new List<WeatherRecord>();
}
