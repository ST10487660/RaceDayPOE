using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Mapping;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Event enrolments: Participants enter events, Organisers view enrolments for their events.</summary>
[Route("api/v1")]
public class EnrolmentsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db) => _db = db;

    /// <summary>Enrols the logged-in Participant in an event with a chosen category (Participant only).</summary>
    /// <remarks>
    /// Send { categoryId }. The category must be offered at the event. A participant can only be enrolled
    /// once per event. If a previous enrolment was cancelled it is re-activated.
    /// </remarks>
    /// <response code="201">Enrolment recorded (participant + event + category).</response>
    /// <response code="400">Invalid body, or the category is not offered at this event.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">Already enrolled, or the event is full.</response>
    [HttpPost("events/{eventId:int}/enrolments")]
    [Authorize(Roles = AppRoles.Participant)]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolmentDto>> Enrol(int eventId, [FromBody] EnrolmentRequest request)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null) return Error(404, "Event not found.");

        var offered = await _db.EventCategories
            .AnyAsync(ec => ec.EventId == eventId && ec.CategoryId == request.CategoryId);
        if (!offered) return Error(400, "The selected category is not offered at this event.");

        var userId = CurrentUserId;
        var existing = await _db.Enrolments.FirstOrDefaultAsync(e => e.EventId == eventId && e.ParticipantId == userId);
        if (existing != null && existing.Status != EnrolmentStatuses.Cancelled)
            return Error(409, "You are already enrolled in this event.");

        var activeCount = await _db.Enrolments
            .CountAsync(e => e.EventId == eventId && e.Status != EnrolmentStatuses.Cancelled);
        if (activeCount >= ev.MaxParticipants)
            return Error(409, "This event is full.");

        Enrolment enrolment;
        if (existing != null)
        {
            existing.CategoryId = request.CategoryId;
            existing.Status = EnrolmentStatuses.Confirmed;
            existing.EnrolmentDate = DateTime.UtcNow;
            enrolment = existing;
        }
        else
        {
            enrolment = new Enrolment
            {
                EventId = eventId,
                CategoryId = request.CategoryId,
                ParticipantId = userId,
                EnrolmentDate = DateTime.UtcNow,
                Status = EnrolmentStatuses.Confirmed
            };
            _db.Enrolments.Add(enrolment);
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Error(409, "You are already enrolled in this event.");
        }

        var dto = await _db.Enrolments.AsNoTracking()
            .Where(e => e.EnrolmentId == enrolment.EnrolmentId)
            .Select(Projections.Enrolment)
            .FirstAsync();

        return Created("/api/v1/enrolments/me", dto);
    }

    /// <summary>Lists the logged-in Participant's own enrolments (Participant only).</summary>
    /// <response code="200">The participant's enrolments, newest first.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    [HttpGet("enrolments/me")]
    [Authorize(Roles = AppRoles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<EnrolmentDto>>> GetMyEnrolments()
    {
        var userId = CurrentUserId;
        var list = await _db.Enrolments.AsNoTracking()
            .Where(e => e.ParticipantId == userId)
            .OrderByDescending(e => e.EnrolmentDate)
            .Select(Projections.Enrolment)
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>Lists all enrolments for an event you own (Organiser only).</summary>
    /// <response code="200">Enrolments with participant and category details.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or the event belongs to another Organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId:int}/enrolments")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<EnrolmentDto>>> GetEventEnrolments(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null) return Error(404, "Event not found.");
        if (ev.OrganiserId != CurrentUserId) return Error(403, "You can only view enrolments for your own events.");

        var list = await _db.Enrolments.AsNoTracking()
            .Where(e => e.EventId == eventId)
            .OrderBy(e => e.EnrolmentDate)
            .Select(Projections.Enrolment)
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>Cancels one of your own enrolments (Participant only).</summary>
    /// <remarks>The enrolment is kept with Status = Cancelled so history is preserved and the spot is freed.</remarks>
    /// <response code="204">Enrolment cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser, or the enrolment belongs to someone else.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result has already been recorded for this enrolment.</response>
    [HttpDelete("enrolments/{id:int}")]
    [Authorize(Roles = AppRoles.Participant)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var enrolment = await _db.Enrolments.FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment == null) return Error(404, "Enrolment not found.");
        if (enrolment.ParticipantId != CurrentUserId) return Error(403, "You can only cancel your own enrolments.");

        if (await _db.Results.AnyAsync(r => r.EnrolmentId == id))
            return Error(409, "A result has been recorded for this enrolment, so it cannot be cancelled.");

        enrolment.Status = EnrolmentStatuses.Cancelled;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
