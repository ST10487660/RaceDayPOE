using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>The id of the user stored in the session cookie.</summary>
    protected int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Returns the standard { statusCode, message } error body.</summary>
    protected ObjectResult Error(int statusCode, string message) =>
        StatusCode(statusCode, new ErrorResponse(statusCode, message));
}
