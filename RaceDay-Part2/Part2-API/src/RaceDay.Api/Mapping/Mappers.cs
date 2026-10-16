using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Mapping;

public static class UserMapper
{
    public static UserProfileDto ToProfile(User u) => new()
    {
        UserId = u.UserId,
        FullName = u.FullName,
        Email = u.Email,
        Role = u.Role,
        CreatedAt = u.CreatedAt
    };
}

public static class EventMapper
{
    public static EventDto ToDto(Event e, int enrolledCount) => new()
    {
        EventId = e.EventId,
        OrganiserId = e.OrganiserId,
        EventName = e.EventName,
        Description = e.Description,
        EventDate = e.EventDate,
        Location = e.Location,
        EventType = e.EventType,
        DistanceKm = e.DistanceKm,
        MaxParticipants = e.MaxParticipants,
        EnrolledCount = enrolledCount,
        Categories = e.EventCategories
            .Select(ec => ToCategoryDto(ec.Category))
            .OrderBy(c => c.CategoryName)
            .ToList(),
        Route = e.Route == null ? null : ToRouteDto(e.Route)
    };

    public static CategoryDto ToCategoryDto(Category c) => new()
    {
        CategoryId = c.CategoryId,
        CategoryName = c.CategoryName,
        Description = c.Description
    };

    public static RouteDto ToRouteDto(EventRoute r) => new()
    {
        RouteId = r.RouteId,
        RouteName = r.RouteName,
        DistanceKm = r.DistanceKm,
        StartPoint = r.StartPoint,
        EndPoint = r.EndPoint
    };

    public static WeatherDto ToWeatherDto(WeatherRecord w) => new()
    {
        WeatherId = w.WeatherId,
        EventId = w.EventId,
        RecordedAt = w.RecordedAt,
        TemperatureC = w.TemperatureC,
        Conditions = w.Conditions,
        WindSpeedKph = w.WindSpeedKph
    };

    /// <summary>Counts active (non-cancelled) enrolments per event id.</summary>
    public static Task<Dictionary<int, int>> CountActiveEnrolmentsAsync(RaceDayDbContext db, List<int> eventIds) =>
        db.Enrolments.AsNoTracking()
            .Where(e => eventIds.Contains(e.EventId) && e.Status != EnrolmentStatuses.Cancelled)
            .GroupBy(e => e.EventId)
            .Select(g => new { EventId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventId, x => x.Count);
}

/// <summary>EF projections shared by several queries.</summary>
public static class Projections
{
    public static readonly Expression<Func<Enrolment, EnrolmentDto>> Enrolment = e => new EnrolmentDto
    {
        EnrolmentId = e.EnrolmentId,
        EventId = e.EventId,
        EventName = e.EventCategory.Event.EventName,
        EventDate = e.EventCategory.Event.EventDate,
        CategoryId = e.CategoryId,
        CategoryName = e.EventCategory.Category.CategoryName,
        ParticipantId = e.ParticipantId,
        ParticipantName = e.Participant.FullName,
        ParticipantEmail = e.Participant.Email,
        EnrolmentDate = e.EnrolmentDate,
        Status = e.Status
    };

    public static readonly Expression<Func<RaceResult, ResultDto>> Result = r => new ResultDto
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        EventId = r.Enrolment.EventId,
        EventName = r.Enrolment.EventCategory.Event.EventName,
        EventDate = r.Enrolment.EventCategory.Event.EventDate,
        ParticipantId = r.Enrolment.ParticipantId,
        ParticipantName = r.Enrolment.Participant.FullName,
        CategoryName = r.Enrolment.EventCategory.Category.CategoryName,
        Position = r.Position,
        FinishTime = r.FinishTime,
        DistanceKm = r.DistanceKm,
        RecordedAt = r.RecordedAt
    };
}
