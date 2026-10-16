namespace RaceDay.Api.Models;

/// <summary>Maps to the "Routes" table (named EventRoute to avoid clashing with MVC's Route attribute).</summary>
public class EventRoute
{
    public int RouteId { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    public string RouteName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string StartPoint { get; set; } = string.Empty;
    public string EndPoint { get; set; } = string.Empty;
}
