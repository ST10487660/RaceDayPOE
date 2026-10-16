using System.Text.Json.Serialization;

namespace RaceDay.Api.Dtos;

/// <summary>Standard error body returned for every failed request (see Part 1 plan: "Error Response Format").</summary>
public class ErrorResponse
{
    public ErrorResponse() { }

    public ErrorResponse(int statusCode, string message, IDictionary<string, string[]>? errors = null)
    {
        StatusCode = statusCode;
        Message = message;
        Errors = errors;
    }

    /// <example>400</example>
    public int StatusCode { get; set; }

    /// <example>Invalid request data.</example>
    public string Message { get; set; } = string.Empty;

    /// <summary>Per-field validation messages (only present for 400 validation errors).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, string[]>? Errors { get; set; }
}
