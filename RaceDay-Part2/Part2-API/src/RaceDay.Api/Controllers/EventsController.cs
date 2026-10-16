using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Mapping;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Events: browsing is public, managing is for Organisers (own events only).</summary>
[Route("api/v1/events")]
public class EventsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db) => _db = db;

    /// <summary>Lists upcoming events (Public).</summary>
    /// <remarks>Pass includePast=true to also see events whose date has passed.</remarks>
    /// <response code="200">Events ordered by date, including their categories and route.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents([FromQuery] bool includePast = false)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        IQueryable<Event> query = _db.Events.AsNoTracking()
            .Include(e => e.EventCategories).ThenInclude(ec => ec.Category)
            .Include(e => e.Route);

        if (!includePast)
            query = query.Where(e => e.EventDate >= today);

        var events = await query.OrderBy(e => e.EventDate).ToListAsync();
        var counts = await EventMapper.CountActiveEnrolmentsAsync(_db, events.Select(e => e.EventId).ToList());

        return Ok(events.Select(e => EventMapper.ToDto(e, counts.GetValueOrDefault(e.EventId))));
    }

    /// <summary>Returns one event with its categories and route (Public).</summary>
    /// <response code="200">The event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> GetEvent(int id)
    {
        var dto = await LoadEventDto(id);
        return dto == null ? Error(404, "Event not found.") : Ok(dto);
    }

    /// <summary>Creates an event owned by the logged-in Organiser (Organiser only).</summary>
    /// <remarks>
    /// Send eventName, description, eventDate (yyyy-MM-dd), location, eventType (Run, Walk or Cycle),
    /// distanceKm, maxParticipants and categoryIds (existing category ids).
    /// </remarks>
    /// <response code="201">Event created.</response>
    /// <response code="400">Invalid data, unknown eventType or unknown categoryIds.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    [HttpPost]
    [Authorize(Roles = AppRoles.Organiser)]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventDto>> CreateEvent([FromBody] EventRequest request)
    {
        var eventType = EventTypes.Normalise(request.EventType);
        if (eventType == null) return Error(400, "EventType must be one of: Run, Walk, Cycle.");

        var categories = await LoadCategories(request.CategoryIds);
        if (categories == null) return Error(400, "One or more categoryIds do not exist.");

        var ev = new Event
        {
            OrganiserId = CurrentUserId,
            EventName = request.EventName.Trim(),
            Description = request.Description?.Trim(),
            EventDate = request.EventDate!.Value,
            Location = request.Location.Trim(),
            EventType = eventType,
            DistanceKm = request.DistanceKm,
            MaxParticipants = request.MaxParticipants,
            CreatedAt = DateTime.UtcNow
        };
        foreach (var category in categories)
            ev.EventCategories.Add(new EventCategory { Category = category });

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        var dto = await LoadEventDto(ev.EventId);
        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventId }, dto);
    }

    /// <summary>Updates an event you own (Organiser only).</summary>
    /// <remarks>Send the full event body. categoryIds replaces the event's categories.</remarks>
    /// <response code="200">Event updated.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or the event belongs to another Organiser.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">Change conflicts with existing enrolments (category removed or capacity too low).</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EventDto>> UpdateEvent(int id, [FromBody] EventRequest request)
    {
        var ev = await _db.Events.Include(e => e.EventCategories).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null) return Error(404, "Event not found.");
        if (ev.OrganiserId != CurrentUserId) return Error(403, "You can only modify your own events.");

        var eventType = EventTypes.Normalise(request.EventType);
        if (eventType == null) return Error(400, "EventType must be one of: Run, Walk, Cycle.");

        var categories = await LoadCategories(request.CategoryIds);
        if (categories == null) return Error(400, "One or more categoryIds do not exist.");

        var activeEnrolments = await _db.Enrolments
            .CountAsync(e => e.EventId == id && e.Status != EnrolmentStatuses.Cancelled);
        if (request.MaxParticipants < activeEnrolments)
            return Error(409, "MaxParticipants cannot be lower than the number of current enrolments.");

        var wantedIds = categories.Select(c => c.CategoryId).ToHashSet();
        var toRemove = ev.EventCategories.Where(ec => !wantedIds.Contains(ec.CategoryId)).ToList();
        var removeIds = toRemove.Select(ec => ec.CategoryId).ToList();
        if (removeIds.Count > 0 &&
            await _db.Enrolments.AnyAsync(e => e.EventId == id && removeIds.Contains(e.CategoryId)))
        {
            return Error(409, "A category with existing enrolments cannot be removed from the event.");
        }

        var existingIds = ev.EventCategories.Select(ec => ec.CategoryId).ToHashSet();
        var toAdd = categories.Where(c => !existingIds.Contains(c.CategoryId)).ToList();

        ev.EventName = request.EventName.Trim();
        ev.Description = request.Description?.Trim();
        ev.EventDate = request.EventDate!.Value;
        ev.Location = request.Location.Trim();
        ev.EventType = eventType;
        ev.DistanceKm = request.DistanceKm;
        ev.MaxParticipants = request.MaxParticipants;

        _db.EventCategories.RemoveRange(toRemove);
        foreach (var category in toAdd)
            _db.EventCategories.Add(new EventCategory { EventId = id, CategoryId = category.CategoryId });

        await _db.SaveChangesAsync();
        return Ok(await LoadEventDto(id));
    }

    /// <summary>Deletes an event you own (Organiser only).</summary>
    /// <response code="204">Event deleted (with its categories links, route and weather).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or the event belongs to another Organiser.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">The event has enrolments, so it cannot be deleted.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null) return Error(404, "Event not found.");
        if (ev.OrganiserId != CurrentUserId) return Error(403, "You can only delete your own events.");

        if (await _db.Enrolments.AnyAsync(e => e.EventId == id))
            return Error(409, "This event has enrolments and cannot be deleted.");

        _db.EventCategories.RemoveRange(_db.EventCategories.Where(ec => ec.EventId == id));
        _db.Routes.RemoveRange(_db.Routes.Where(r => r.EventId == id));
        _db.WeatherRecords.RemoveRange(_db.WeatherRecords.Where(w => w.EventId == id));
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Lists the categories offered at an event (Public).</summary>
    /// <response code="200">The event's categories.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{eventId:int}/categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetEventCategories(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return Error(404, "Event not found.");

        var categories = await _db.EventCategories.AsNoTracking()
            .Where(ec => ec.EventId == eventId)
            .Select(ec => ec.Category)
            .OrderBy(c => c.CategoryName)
            .ToListAsync();

        return Ok(categories.Select(EventMapper.ToCategoryDto));
    }

    /// <summary>Returns the route for an event (Public).</summary>
    /// <response code="200">Route name, distance, start and end point.</response>
    /// <response code="404">Event or route not found.</response>
    [HttpGet("{eventId:int}/route")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RouteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RouteDto>> GetRoute(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return Error(404, "Event not found.");

        var route = await _db.Routes.AsNoTracking().FirstOrDefaultAsync(r => r.EventId == eventId);
        return route == null ? Error(404, "No route has been published for this event.") : Ok(EventMapper.ToRouteDto(route));
    }

    /// <summary>Returns the latest weather record for an event (Public).</summary>
    /// <response code="200">Temperature, conditions and wind speed.</response>
    /// <response code="404">Event or weather information not found.</response>
    [HttpGet("{eventId:int}/weather")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(WeatherDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WeatherDto>> GetWeather(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return Error(404, "Event not found.");

        var weather = await _db.WeatherRecords.AsNoTracking()
            .Where(w => w.EventId == eventId)
            .OrderByDescending(w => w.RecordedAt)
            .FirstOrDefaultAsync();

        return weather == null ? Error(404, "No weather information is available for this event.") : Ok(EventMapper.ToWeatherDto(weather));
    }

    // ---------- helpers ----------

    private async Task<EventDto?> LoadEventDto(int id)
    {
        var ev = await _db.Events.AsNoTracking()
            .Include(e => e.EventCategories).ThenInclude(ec => ec.Category)
            .Include(e => e.Route)
            .FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null) return null;

        var counts = await EventMapper.CountActiveEnrolmentsAsync(_db, new List<int> { id });
        return EventMapper.ToDto(ev, counts.GetValueOrDefault(id));
    }

    /// <summary>Loads the requested categories; returns null if any id does not exist.</summary>
    private async Task<List<Category>?> LoadCategories(IEnumerable<int> categoryIds)
    {
        var ids = categoryIds.Distinct().ToList();
        var categories = await _db.Categories.Where(c => ids.Contains(c.CategoryId)).ToListAsync();
        return categories.Count == ids.Count ? categories : null;
    }
}
