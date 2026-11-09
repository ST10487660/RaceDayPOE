namespace RaceDay.Web.Services;

/// <summary>The outcome of one call to the RaceDay API: data on success, a friendly message on failure.</summary>
public class ApiResult<T>
{
    public bool Success { get; init; }
    public int Status { get; init; }
    public T? Data { get; init; }
    public string Message { get; init; } = string.Empty;
    public IDictionary<string, string[]> Errors { get; init; } = new Dictionary<string, string[]>();

    public static ApiResult<T> Ok(T? data, int status = 200) => new() { Success = true, Status = status, Data = data };

    public static ApiResult<T> Fail(int status, string message, IDictionary<string, string[]>? errors = null) =>
        new() { Status = status, Message = message, Errors = errors ?? new Dictionary<string, string[]>() };
}
