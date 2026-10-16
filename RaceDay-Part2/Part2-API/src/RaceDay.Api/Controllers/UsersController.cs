using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Infrastructure;
using RaceDay.Api.Mapping;

namespace RaceDay.Api.Controllers;

/// <summary>The logged-in user's own profile.</summary>
[Route("api/v1/users")]
[Authorize]
public class UsersController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public UsersController(RaceDayDbContext db) => _db = db;

    /// <summary>Returns the profile of the logged-in user (Organiser or Participant).</summary>
    /// <response code="200">The user's profile.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> GetMe()
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
        if (user == null) return Error(401, "Your account no longer exists. Please log in again.");
        return Ok(UserMapper.ToProfile(user));
    }

    /// <summary>Updates the logged-in user's name and email (Organiser or Participant).</summary>
    /// <remarks>Send { fullName, email }. The role cannot be changed here.</remarks>
    /// <response code="200">Profile updated. Returns the new profile.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="409">Another account already uses this email.</response>
    [HttpPut("me")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserProfileDto>> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var userId = CurrentUserId;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return Error(401, "Your account no longer exists. Please log in again.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email && u.UserId != userId))
            return Error(409, "Another account already uses this email.");

        user.FullName = request.FullName.Trim();
        user.Email = email;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Error(409, "Another account already uses this email.");
        }

        // Refresh the session so the name/email in the cookie match the database.
        await SessionSignIn.SignInAsync(HttpContext, user);
        return Ok(UserMapper.ToProfile(user));
    }
}
