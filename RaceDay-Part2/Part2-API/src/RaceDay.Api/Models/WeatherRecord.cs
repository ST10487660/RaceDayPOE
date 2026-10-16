namespace RaceDay.Api.Models;

/// <summary>Maps to the "Weather" table.</summary>
public class WeatherRecord
{
    public int WeatherId { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    public DateTime RecordedAt { get; set; }
    public decimal? TemperatureC { get; set; }
    public string? Conditions { get; set; }
    public decimal? WindSpeedKph { get; set; }
}
