using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Services;

/// <summary>
/// Typed client for the Part 2 RaceDay API. It forwards the API's session cookie (kept in this app's server-side
/// session) on every call, so the API still decides who the user is and what role they have.
/// </summary>
public class ApiClient
{
    private const string ApiCookieName = "RaceDay.Session";
    private const string ServiceDownMessage = "The RaceDay service is temporarily unavailable. Please try again in a moment.";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;

    public ApiClient(HttpClient http, IHttpContextAccessor accessor)
    {
        _http = http;
        _accessor = accessor;
    }

    private ISession Session => _accessor.HttpContext!.Session;

    // ------------------------------------------------------------------ auth + profile

    public Task<ApiResult<ApiUser>> RegisterAsync(string fullName, string email, string password, string role, string? inviteCode) =>
        SendAsync<ApiUser>(HttpMethod.Post, "api/v1/auth/register",
            JsonContent.Create(new { fullName, email, password, role, organiserInviteCode = inviteCode }), attachSession: false);

    public Task<ApiResult<ApiUser>> LoginAsync(string email, string password) =>
        SendAsync<ApiUser>(HttpMethod.Post, "api/v1/auth/login",
            JsonContent.Create(new { email, password }), attachSession: false);

    public Task<ApiResult<bool>> LogoutAsync() => SendNoContentAsync(HttpMethod.Post, "api/v1/auth/logout");

    public Task<ApiResult<ApiUser>> GetMeAsync() => SendAsync<ApiUser>(HttpMethod.Get, "api/v1/users/me");

    public Task<ApiResult<ApiUser>> UpdateMeAsync(string fullName, string email) =>
        SendAsync<ApiUser>(HttpMethod.Put, "api/v1/users/me", JsonContent.Create(new { fullName, email }));

    public Task<ApiResult<ApiUser>> UploadPictureAsync(IFormFile file) =>
        SendAsync<ApiUser>(HttpMethod.Post, "api/v1/users/me/picture", ImageContent(file));

    // ------------------------------------------------------------------ events

    public Task<ApiResult<List<ApiEvent>>> GetEventsAsync(string? search = null, string? eventType = null, DateOnly? from = null, DateOnly? to = null)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) query.Add("search=" + Uri.EscapeDataString(search.Trim()));
        if (!string.IsNullOrWhiteSpace(eventType)) query.Add("eventType=" + Uri.EscapeDataString(eventType));
        if (from.HasValue) query.Add("from=" + from.Value.ToString("yyyy-MM-dd"));
        if (to.HasValue) query.Add("to=" + to.Value.ToString("yyyy-MM-dd"));
        var url = "api/v1/events" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);
        return SendAsync<List<ApiEvent>>(HttpMethod.Get, url);
    }

    public Task<ApiResult<List<ApiEvent>>> GetMyEventsAsync() => SendAsync<List<ApiEvent>>(HttpMethod.Get, "api/v1/events/mine");

    public Task<ApiResult<ApiEvent>> GetEventAsync(int id) => SendAsync<ApiEvent>(HttpMethod.Get, $"api/v1/events/{id}");

    public Task<ApiResult<ApiWeather>> GetWeatherAsync(int eventId) => SendAsync<ApiWeather>(HttpMethod.Get, $"api/v1/events/{eventId}/weather");

    public Task<ApiResult<ApiEvent>> CreateEventAsync(ApiEventRequest request) =>
        SendAsync<ApiEvent>(HttpMethod.Post, "api/v1/events", JsonContent.Create(request));

    public Task<ApiResult<ApiEvent>> UpdateEventAsync(int id, ApiEventRequest request) =>
        SendAsync<ApiEvent>(HttpMethod.Put, $"api/v1/events/{id}", JsonContent.Create(request));

    public Task<ApiResult<bool>> DeleteEventAsync(int id) => SendNoContentAsync(HttpMethod.Delete, $"api/v1/events/{id}");

    public Task<ApiResult<ApiEvent>> UploadBannerAsync(int eventId, IFormFile file) =>
        SendAsync<ApiEvent>(HttpMethod.Post, $"api/v1/events/{eventId}/banner", ImageContent(file));

    // ------------------------------------------------------------------ categories

    public Task<ApiResult<List<ApiCategory>>> GetCategoriesAsync() => SendAsync<List<ApiCategory>>(HttpMethod.Get, "api/v1/categories");

    public Task<ApiResult<ApiCategory>> CreateCategoryAsync(string name, string? description) =>
        SendAsync<ApiCategory>(HttpMethod.Post, "api/v1/categories", JsonContent.Create(new { categoryName = name, description }));

    public Task<ApiResult<ApiCategory>> UpdateCategoryAsync(int id, string name, string? description) =>
        SendAsync<ApiCategory>(HttpMethod.Put, $"api/v1/categories/{id}", JsonContent.Create(new { categoryName = name, description }));

    public Task<ApiResult<bool>> DeleteCategoryAsync(int id) => SendNoContentAsync(HttpMethod.Delete, $"api/v1/categories/{id}");

    // ------------------------------------------------------------------ enrolments

    public Task<ApiResult<ApiEnrolment>> EnrolAsync(int eventId, int categoryId) =>
        SendAsync<ApiEnrolment>(HttpMethod.Post, $"api/v1/events/{eventId}/enrolments", JsonContent.Create(new { categoryId }));

    public Task<ApiResult<List<ApiEnrolment>>> GetMyEnrolmentsAsync() =>
        SendAsync<List<ApiEnrolment>>(HttpMethod.Get, "api/v1/enrolments/me");

    public Task<ApiResult<List<ApiEnrolment>>> GetEventEnrolmentsAsync(int eventId) =>
        SendAsync<List<ApiEnrolment>>(HttpMethod.Get, $"api/v1/events/{eventId}/enrolments");

    public Task<ApiResult<bool>> CancelEnrolmentAsync(int enrolmentId) =>
        SendNoContentAsync(HttpMethod.Delete, $"api/v1/enrolments/{enrolmentId}");

    // ------------------------------------------------------------------ results

    public Task<ApiResult<List<ApiResultItem>>> GetMyResultsAsync() =>
        SendAsync<List<ApiResultItem>>(HttpMethod.Get, "api/v1/results/me");

    public Task<ApiResult<List<ApiResultItem>>> GetEventResultsAsync(int eventId) =>
        SendAsync<List<ApiResultItem>>(HttpMethod.Get, $"api/v1/events/{eventId}/results");

    public Task<ApiResult<ApiResultItem>> CaptureResultAsync(int eventId, int enrolmentId, int position, TimeSpan finishTime, decimal? distanceKm) =>
        SendAsync<ApiResultItem>(HttpMethod.Post, $"api/v1/events/{eventId}/results",
            JsonContent.Create(new { enrolmentId, position, finishTime, distanceKm }));

    public Task<ApiResult<ApiResultItem>> UpdateResultAsync(int resultId, int position, TimeSpan finishTime, decimal? distanceKm) =>
        SendAsync<ApiResultItem>(HttpMethod.Put, $"api/v1/results/{resultId}",
            JsonContent.Create(new { position, finishTime, distanceKm }));

    // ------------------------------------------------------------------ plumbing

    private static MultipartFormDataContent ImageContent(IFormFile file)
    {
        var form = new MultipartFormDataContent();
        var stream = new StreamContent(file.OpenReadStream());
        if (!string.IsNullOrWhiteSpace(file.ContentType))
            stream.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        form.Add(stream, "file", file.FileName);
        return form;
    }

    private async Task<HttpResponseMessage> RequestAsync(HttpMethod method, string url, HttpContent? content, bool attachSession)
    {
        using var request = new HttpRequestMessage(method, url) { Content = content };

        if (attachSession)
        {
            var cookie = Session.GetString(SessionKeys.ApiCookie);
            if (!string.IsNullOrEmpty(cookie)) request.Headers.Add("Cookie", cookie);
        }

        var response = await _http.SendAsync(request);
        RememberApiCookie(response);
        return response;
    }

    /// <summary>Keeps the API's session cookie (issued at login, and refreshed by the API) in our own session.</summary>
    private void RememberApiCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values)) return;

        foreach (var header in values)
        {
            if (!header.StartsWith(ApiCookieName + "=", StringComparison.Ordinal)) continue;
            var pair = header.Split(';', 2)[0];
            if (pair.Length > ApiCookieName.Length + 1) // ignore the empty value the API sends when logging out
                Session.SetString(SessionKeys.ApiCookie, pair);
        }
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content = null, bool attachSession = true)
    {
        try
        {
            using var response = await RequestAsync(method, url, content, attachSession);
            if (!response.IsSuccessStatusCode) return await FailureAsync<T>(response);

            var data = await response.Content.ReadFromJsonAsync<T>(Json);
            return ApiResult<T>.Ok(data, (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or NotSupportedException)
        {
            return ApiResult<T>.Fail(503, ServiceDownMessage);
        }
    }

    private async Task<ApiResult<bool>> SendNoContentAsync(HttpMethod method, string url, HttpContent? content = null)
    {
        try
        {
            using var response = await RequestAsync(method, url, content, attachSession: true);
            return response.IsSuccessStatusCode
                ? ApiResult<bool>.Ok(true, (int)response.StatusCode)
                : await FailureAsync<bool>(response);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ApiResult<bool>.Fail(503, ServiceDownMessage);
        }
    }

    private static async Task<ApiResult<T>> FailureAsync<T>(HttpResponseMessage response)
    {
        var status = (int)response.StatusCode;
        ApiError? error = null;
        try
        {
            error = await response.Content.ReadFromJsonAsync<ApiError>(Json);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            // not a RaceDay error body; fall through to the generic message
        }

        var message = !string.IsNullOrWhiteSpace(error?.Message) ? error!.Message : status switch
        {
            401 => "Please log in to continue.",
            403 => "You do not have permission to do that.",
            404 => "We could not find what you were looking for.",
            _ => "Something went wrong while contacting the RaceDay service. Please try again."
        };

        return ApiResult<T>.Fail(status, message, error?.Errors);
    }
}
