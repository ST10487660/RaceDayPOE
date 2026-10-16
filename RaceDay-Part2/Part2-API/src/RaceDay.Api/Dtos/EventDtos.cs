using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

/// <summary>Body used to create or fully update an event.</summary>
public class EventRequest
{
    /// <example>Johannesburg Spring Run</example>
    [Required, StringLength(150)]
    public string EventName { get; set; } = string.Empty;

    /// <example>Community road-running event.</example>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>ISO date, e.g. 2026-10-18.</summary>
    /// <example>2026-12-05</example>
    [Required]
    public DateOnly? EventDate { get; set; }

    /// <example>Johannesburg, Gauteng</example>
    [Required, StringLength(200)]
    public string Location { get; set; } = string.Empty;

    /// <summary>Run, Walk or Cycle.</summary>
    /// <example>Run</example>
    [Required]
    public string EventType { get; set; } = string.Empty;

    /// <summary>Headline distance of the event in kilometres.</summary>
    /// <example>10</example>
    [Range(0.01, 1000)]
    public decimal DistanceKm { get; set; }

    /// <example>500</example>
    [Range(1, 1000000)]
    public int MaxParticipants { get; set; }

    /// <summary>IDs of the categories offered at this event (see GET /api/v1/categories).</summary>
    public List<int> CategoryIds { get; set; } = new();
}

public class EventDto
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
    /// <summary>Number of active (non-cancelled) enrolments.</summary>
    public int EnrolledCount { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();
    public RouteDto? Route { get; set; }
}

public class CategoryRequest
{
    /// <example>Under 20</example>
    [Required, StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    /// <example>Runners aged 19 and under.</example>
    [StringLength(300)]
    public string? Description { get; set; }
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class RouteDto
{
    public int RouteId { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string StartPoint { get; set; } = string.Empty;
    public string EndPoint { get; set; } = string.Empty;
}

public class WeatherDto
{
    public int WeatherId { get; set; }
    public int EventId { get; set; }
    public DateTime RecordedAt { get; set; }
    public decimal? TemperatureC { get; set; }
    public string? Conditions { get; set; }
    public decimal? WindSpeedKph { get; set; }
}
