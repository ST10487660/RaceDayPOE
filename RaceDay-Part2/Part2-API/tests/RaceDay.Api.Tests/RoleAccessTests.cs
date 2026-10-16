using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests;

/// <summary>Role-based access: the wrong role gets 403, no session gets 401.</summary>
public class RoleAccessTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public RoleAccessTests(RaceDayFactory factory) => _factory = factory;

    private static HttpRequestMessage Build(string method, string url)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST" or "PUT")
            request.Content = JsonContent.Create(new { });
        return request;
    }

    [Theory]
    [InlineData("POST", "/api/v1/events")]
    [InlineData("PUT", "/api/v1/events/1")]
    [InlineData("DELETE", "/api/v1/events/1")]
    [InlineData("POST", "/api/v1/categories")]
    [InlineData("PUT", "/api/v1/categories/1")]
    [InlineData("DELETE", "/api/v1/categories/1")]
    [InlineData("GET", "/api/v1/events/1/enrolments")]
    [InlineData("GET", "/api/v1/events/1/results")]
    [InlineData("POST", "/api/v1/events/1/results")]
    [InlineData("PUT", "/api/v1/results/1")]
    [InlineData("DELETE", "/api/v1/results/1")]
    public async Task Participant_IsRejected_FromOrganiserEndpoints(string method, string url)
    {
        var participant = await _factory.SignUpAsync(AppRoles.Participant);

        var response = await participant.Client.SendAsync(Build(method, url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/v1/events/1/enrolments")]
    [InlineData("GET", "/api/v1/enrolments/me")]
    [InlineData("DELETE", "/api/v1/enrolments/1")]
    [InlineData("GET", "/api/v1/results/me")]
    public async Task Organiser_IsRejected_FromParticipantEndpoints(string method, string url)
    {
        var organiser = await _factory.SignUpAsync(AppRoles.Organiser);

        var response = await organiser.Client.SendAsync(Build(method, url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/v1/users/me")]
    [InlineData("PUT", "/api/v1/users/me")]
    [InlineData("POST", "/api/v1/auth/logout")]
    [InlineData("POST", "/api/v1/events")]
    [InlineData("PUT", "/api/v1/events/1")]
    [InlineData("DELETE", "/api/v1/events/1")]
    [InlineData("POST", "/api/v1/categories")]
    [InlineData("POST", "/api/v1/events/1/enrolments")]
    [InlineData("GET", "/api/v1/enrolments/me")]
    [InlineData("GET", "/api/v1/events/1/enrolments")]
    [InlineData("DELETE", "/api/v1/enrolments/1")]
    [InlineData("GET", "/api/v1/results/me")]
    [InlineData("GET", "/api/v1/events/1/results")]
    [InlineData("POST", "/api/v1/events/1/results")]
    public async Task Unauthenticated_IsRejected_FromProtectedEndpoints(string method, string url)
    {
        var anonymous = _factory.NewClient();

        var response = await anonymous.SendAsync(Build(method, url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/events")]
    [InlineData("/api/v1/categories")]
    public async Task PublicEndpoints_AreOpenToAnonymousUsers(string url)
    {
        var response = await _factory.NewClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
