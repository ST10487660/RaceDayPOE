namespace RaceDay.Web.Models.Api;

// Shapes of the JSON returned by the Part 2 API. These are plain data holders: no business logic lives here.

public class ApiError
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string[]>? Errors { get; set; }
}

public class ApiUser
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ApiCategory
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ApiRoute
{
    public int RouteId { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string StartPoint { get; set; } = string.Empty;
    public string EndPoint { get; set; } = string.Empty;
}

public class ApiWeather
{
    public DateTime RecordedAt { get; set; }
    public decimal? TemperatureC { get; set; }
    public string? Conditions { get; set; }
    public decimal? WindSpeedKph { get; set; }
}

public class ApiEvent
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
    public int EnrolledCount { get; set; }
    public string? BannerImageUrl { get; set; }
    public List<ApiCategory> Categories { get; set; } = new();
    public ApiRoute? Route { get; set; }
}

public class ApiEventRequest
{
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly? EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
    public List<int> CategoryIds { get; set; } = new();
}

public class ApiEnrolment
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ApiResultItem
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int? Position { get; set; }
    public int TotalFinishers { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public decimal? DistanceKm { get; set; }
    public DateTime RecordedAt { get; set; }
}
