using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Mapping;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Race results: Organisers capture them, Participants view their own.</summary>
[Route("api/v1")]
public class ResultsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db) => _db = db;

    /// <summary>Returns the logged-in Participant's own result history (Participant only).</summary>
    /// <response code="200">Only results belonging to the logged-in participant.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    [HttpGet("results/me")]
    [Authorize(Roles = AppRoles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ResultDto>>> GetMyResults()
    {
        var userId = CurrentUserId;
        var list = await _db.Results.AsNoTracking()
            .Where(r => r.Enrolment.ParticipantId == userId)
            .OrderByDescending(r => r.RecordedAt)
            .Select(Projections.Result)
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>Lists all results for an event you own (Organiser only).</summary>
    /// <response code="200">Results ordered by position.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or the event belongs to another Organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId:int}/results")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ResultDto>>> GetEventResults(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null) return Error(404, "Event not found.");
        if (ev.OrganiserId != CurrentUserId) return Error(403, "You can only view results for your own events.");

        var list = await _db.Results.AsNoTracking()
            .Where(r => r.Enrolment.EventId == eventId)
            .OrderBy(r => r.Position)
            .Select(Projections.Result)
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>Captures a participant's result for an event you own (Organiser only).</summary>
    /// <remarks>Send { enrolmentId, position, finishTime (hh:mm:ss), distanceKm }. One result per enrolment.</remarks>
    /// <response code="201">Result recorded.</response>
    /// <response code="400">Invalid data, or the enrolment was cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or the event belongs to another Organiser.</response>
    /// <response code="404">Event, or enrolment for this event, not found.</response>
    /// <response code="409">A result already exists for this enrolment.</response>
    [HttpPost("events/{eventId:int}/results")]
    [Authorize(Roles = AppRoles.Organiser)]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultDto>> CaptureResult(int eventId, [FromBody] CreateResultRequest request)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null) return Error(404, "Event not found.");
        if (ev.OrganiserId != CurrentUserId) return Error(403, "You can only capture results for your own events.");

        var timeError = ValidateFinishTime(request.FinishTime);
        if (timeError != null) return Error(400, timeError);

        var enrolment = await _db.Enrolments
            .FirstOrDefaultAsync(e => e.EnrolmentId == request.EnrolmentId && e.EventId == eventId);
        if (enrolment == null) return Error(404, "Enrolment not found for this event.");
        if (enrolment.Status == EnrolmentStatuses.Cancelled)
            return Error(400, "A result cannot be captured for a cancelled enrolment.");

        if (await _db.Results.AnyAsync(r => r.EnrolmentId == enrolment.EnrolmentId))
            return Error(409, "A result has already been captured for this enrolment.");

        var result = new RaceResult
        {
            EnrolmentId = enrolment.EnrolmentId,
            Position = request.Position,
            FinishTime = request.FinishTime,
            DistanceKm = request.DistanceKm,
            RecordedAt = DateTime.UtcNow
        };
        _db.Results.Add(result);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Error(409, "A result has already been captured for this enrolment.");
        }

        var dto = await _db.Results.AsNoTracking()
            .Where(r => r.ResultId == result.ResultId)
            .Select(Projections.Result)
            .FirstAsync();
        return Created($"/api/v1/events/{eventId}/results", dto);
    }

    /// <summary>Corrects an existing result (Organiser only, own events).</summary>
    /// <response code="200">Result updated.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or the result belongs to another Organiser's event.</response>
    /// <response code="404">Result not found.</response>
    [HttpPut("results/{id:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultDto>> UpdateResult(int id, [FromBody] ResultRequest request)
    {
        var result = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e.EventCategory).ThenInclude(ec => ec.Event)
            .FirstOrDefaultAsync(r => r.ResultId == id);
        if (result == null) return Error(404, "Result not found.");
        if (result.Enrolment.EventCategory.Event.OrganiserId != CurrentUserId)
            return Error(403, "You can only modify results for your own events.");

        var timeError = ValidateFinishTime(request.FinishTime);
        if (timeError != null) return Error(400, timeError);

        result.Position = request.Position;
        result.FinishTime = request.FinishTime;
        result.DistanceKm = request.DistanceKm;
        await _db.SaveChangesAsync();

        var dto = await _db.Results.AsNoTracking()
            .Where(r => r.ResultId == id)
            .Select(Projections.Result)
            .FirstAsync();
        return Ok(dto);
    }

    /// <summary>Removes an incorrect result (Organiser only, own events).</summary>
    /// <response code="204">Result deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or the result belongs to another Organiser's event.</response>
    /// <response code="404">Result not found.</response>
    [HttpDelete("results/{id:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteResult(int id)
    {
        var result = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e.EventCategory).ThenInclude(ec => ec.Event)
            .FirstOrDefaultAsync(r => r.ResultId == id);
        if (result == null) return Error(404, "Result not found.");
        if (result.Enrolment.EventCategory.Event.OrganiserId != CurrentUserId)
            return Error(403, "You can only delete results for your own events.");

        _db.Results.Remove(result);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static string? ValidateFinishTime(TimeSpan? finishTime) =>
        finishTime is null || finishTime <= TimeSpan.Zero || finishTime >= TimeSpan.FromHours(24)
            ? "FinishTime must be greater than 00:00:00 and less than 24 hours."
            : null;
}
