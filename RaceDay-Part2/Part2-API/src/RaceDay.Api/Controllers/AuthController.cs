using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Infrastructure;
using RaceDay.Api.Mapping;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;


[Route("api/v1/auth")]
public class AuthController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordHasher<User> _hasher;
    private readonly IConfiguration _config;

    public AuthController(RaceDayDbContext db, IPasswordHasher<User> hasher, IConfiguration config)
    {
        _db = db;
        _hasher = hasher;
        _config = config;
    }

    /// <summary>Registers a new account (Public).</summary>
    /// <remarks>
    /// Send fullName, email and password. Role is "Participant" by default.
    /// To register as an "Organiser" you must also send the secret organiserInviteCode, so
    /// that nobody can simply self-promote to Organiser. The password is stored hashed.
    /// </remarks>
    /// <response code="201">Account created. Returns the new profile.</response>
    /// <response code="400">Validation failed (missing fields, bad email, short password, invalid role).</response>
    /// <response code="403">Organiser role requested without a valid invite code.</response>
    /// <response code="409">An account with this email already exists.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserProfileDto>> Register([FromBody] RegisterRequest request)
    {
        var role = AppRoles.Normalise(request.Role ?? AppRoles.Participant);
        if (role == null)
            return Error(400, "Role must be either 'Organiser' or 'Participant'.");

        if (role == AppRoles.Organiser)
        {
            var expected = _config["Registration:OrganiserInviteCode"];
            if (string.IsNullOrWhiteSpace(expected) || !CodesMatch(request.OrganiserInviteCode, expected))
                return Error(403, "A valid organiser invite code is required to register as an Organiser.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Error(409, "An account with this email already exists.");

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Error(409, "An account with this email already exists.");
        }

        return Created("/api/v1/users/me", UserMapper.ToProfile(user));
    }

    /// <summary>Logs in and starts a session (Public).</summary>
    /// <remarks>
    /// On success the API sets an encrypted, HttpOnly session cookie (RaceDay.Session) that stores the
    /// user's id and role. The browser (and Swagger UI) sends it automatically on later requests.
    /// </remarks>
    /// <response code="200">Logged in. Returns the profile including the role.</response>
    /// <response code="400">Email or password missing.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null ||
            _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return Error(401, "Invalid email or password.");
        }

        await SessionSignIn.SignInAsync(HttpContext, user);
        return Ok(UserMapper.ToProfile(user));
    }

    /// <summary>Logs out and ends the session (any logged-in user).</summary>
    /// <response code="204">Session ended.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    private static bool CodesMatch(string? supplied, string expected) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(supplied ?? string.Empty),
            Encoding.UTF8.GetBytes(expected));
}
