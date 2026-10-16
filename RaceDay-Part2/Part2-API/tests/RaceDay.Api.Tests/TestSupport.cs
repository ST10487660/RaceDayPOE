using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests;

public record TestUser(HttpClient Client, UserProfileDto Profile, string Email, string Password);

public static class TestSupport
{
    public const string Password = "Str0ng!Passw0rd";

    /// <summary>A client with its own cookie jar, i.e. its own session.</summary>
    public static HttpClient NewClient(this RaceDayFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

    /// <summary>Registers a brand-new user with a unique email, logs in, and returns the logged-in client.</summary>
    public static async Task<TestUser> SignUpAsync(this RaceDayFactory factory, string role)
    {
        var client = factory.NewClient();
        var email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@test.raceday.co.za";

        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            fullName = $"Test {role}",
            email,
            password = Password,
            role,
            organiserInviteCode = role == AppRoles.Organiser ? factory.OrganiserInviteCode : null
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var profile = (await login.Content.ReadFromJsonAsync<UserProfileDto>())!;

        return new TestUser(client, profile, email, Password);
    }

    public static async Task<CategoryDto> CreateCategoryAsync(this HttpClient organiser, string? name = null)
    {
        var response = await organiser.PostAsJsonAsync("/api/v1/categories", new
        {
            categoryName = name ?? $"Category {Guid.NewGuid():N}",
            description = "Test category"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    public static object EventBody(IEnumerable<int> categoryIds, int maxParticipants = 100, string name = "Test Fun Run") => new
    {
        eventName = name,
        description = "A test event",
        eventDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30).ToString("yyyy-MM-dd"),
        location = "Pretoria",
        eventType = "Run",
        distanceKm = 10,
        maxParticipants,
        categoryIds
    };

    public static async Task<EventDto> CreateEventAsync(this HttpClient organiser, IEnumerable<int> categoryIds, int maxParticipants = 100)
    {
        var response = await organiser.PostAsJsonAsync("/api/v1/events", EventBody(categoryIds, maxParticipants));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EventDto>())!;
    }

    public static async Task<EnrolmentDto> EnrolAsync(this HttpClient participant, int eventId, int categoryId)
    {
        var response = await participant.PostAsJsonAsync($"/api/v1/events/{eventId}/enrolments", new { categoryId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EnrolmentDto>())!;
    }
}
