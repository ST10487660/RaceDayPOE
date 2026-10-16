using RaceDay.Api.Dtos;

namespace RaceDay.Api.Infrastructure;

public static class ErrorWriter
{
    /// <summary>Writes the standard JSON error body (used for 401/403/500 produced outside controllers).</summary>
    public static Task WriteAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(new ErrorResponse(statusCode, message));
    }
}
