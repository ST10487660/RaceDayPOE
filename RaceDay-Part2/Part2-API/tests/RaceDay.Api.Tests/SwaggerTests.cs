using System.Net;
using System.Text.Json;

namespace RaceDay.Api.Tests;

/// <summary>
/// Live verification that the built API matches the Part 1 endpoint plan:
/// every planned route must appear in the generated Swagger document.
/// </summary>
public class SwaggerTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public SwaggerTests(RaceDayFactory factory) => _factory = factory;

    public static IEnumerable<object[]> PlannedEndpoints() => new[]
    {
        new object[] { "post", "/api/v1/auth/register" },
        new object[] { "post", "/api/v1/auth/login" },
        new object[] { "post", "/api/v1/auth/logout" },
        new object[] { "get", "/api/v1/users/me" },
        new object[] { "put", "/api/v1/users/me" },
        new object[] { "get", "/api/v1/events" },
        new object[] { "get", "/api/v1/events/{id}" },
        new object[] { "post", "/api/v1/events" },
        new object[] { "put", "/api/v1/events/{id}" },
        new object[] { "delete", "/api/v1/events/{id}" },
        new object[] { "get", "/api/v1/categories" },
        new object[] { "post", "/api/v1/categories" },
        new object[] { "put", "/api/v1/categories/{id}" },
        new object[] { "delete", "/api/v1/categories/{id}" },
        new object[] { "get", "/api/v1/events/{eventId}/categories" },
        new object[] { "post", "/api/v1/events/{eventId}/enrolments" },
        new object[] { "get", "/api/v1/enrolments/me" },
        new object[] { "get", "/api/v1/events/{eventId}/enrolments" },
        new object[] { "delete", "/api/v1/enrolments/{id}" },
        new object[] { "get", "/api/v1/results/me" },
        new object[] { "get", "/api/v1/events/{eventId}/results" },
        new object[] { "post", "/api/v1/events/{eventId}/results" },
        new object[] { "put", "/api/v1/results/{id}" },
        new object[] { "delete", "/api/v1/results/{id}" },
        new object[] { "get", "/api/v1/events/{eventId}/route" },
        new object[] { "get", "/api/v1/events/{eventId}/weather" },
    };

    [Theory]
    [MemberData(nameof(PlannedEndpoints))]
    public async Task SwaggerDocument_ContainsPlannedEndpoint(string method, string path)
    {
        var response = await _factory.NewClient().GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = doc.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty(path, out var pathItem), $"Swagger is missing path {path}");
        Assert.True(pathItem.TryGetProperty(method, out _), $"Swagger is missing {method.ToUpperInvariant()} {path}");
    }

    [Fact]
    public async Task SwaggerUi_IsServed()
    {
        var response = await _factory.NewClient().GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
