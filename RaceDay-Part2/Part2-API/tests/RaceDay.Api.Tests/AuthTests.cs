using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests;

/// <summary>User registration and login (success and failure).</summary>
public class AuthTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public AuthTests(RaceDayFactory factory) => _factory = factory;

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@test.raceday.co.za";

    [Fact]
    public async Task Register_Participant_Returns201WithProfile()
    {
        var client = _factory.NewClient();
        var email = NewEmail();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { fullName = "Jane Runner", email, password = TestSupport.Password });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.Equal(AppRoles.Participant, profile!.Role);
        Assert.Equal(email, profile.Email);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlainText()
    {
        var client = _factory.NewClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/v1/auth/register",
            new { fullName = "Hash Check", email, password = TestSupport.Password });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == email);

        Assert.NotEqual(TestSupport.Password, user.PasswordHash);
        Assert.DoesNotContain(TestSupport.Password, user.PasswordHash);
        Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = _factory.NewClient();
        var email = NewEmail();
        var body = new { fullName = "Dup User", email, password = TestSupport.Password };

        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/auth/register", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v1/auth/register", body)).StatusCode);
    }

    [Theory]
    [InlineData("", "valid@example.com", "Password123!")]          // missing name
    [InlineData("Name", "not-an-email", "Password123!")]           // bad email
    [InlineData("Name", "valid@example.com", "short")]             // password too short
    public async Task Register_InvalidData_Returns400(string fullName, string email, string password)
    {
        var response = await _factory.NewClient().PostAsJsonAsync("/api/v1/auth/register",
            new { fullName, email, password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal(400, error!.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var response = await _factory.NewClient().PostAsJsonAsync("/api/v1/auth/register",
            new { fullName = "Bad Role", email = NewEmail(), password = TestSupport.Password, role = "Admin" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_Organiser_WithoutInviteCode_Returns403()
    {
        var response = await _factory.NewClient().PostAsJsonAsync("/api/v1/auth/register",
            new { fullName = "Sneaky", email = NewEmail(), password = TestSupport.Password, role = "Organiser" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_Organiser_WithWrongInviteCode_Returns403()
    {
        var response = await _factory.NewClient().PostAsJsonAsync("/api/v1/auth/register",
            new { fullName = "Sneaky", email = NewEmail(), password = TestSupport.Password, role = "Organiser", organiserInviteCode = "WRONG" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_Organiser_WithValidInviteCode_Returns201()
    {
        var response = await _factory.NewClient().PostAsJsonAsync("/api/v1/auth/register",
            new
            {
                fullName = "Real Organiser",
                email = NewEmail(),
                password = TestSupport.Password,
                role = "Organiser",
                organiserInviteCode = _factory.OrganiserInviteCode
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.Equal(AppRoles.Organiser, profile!.Role);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithRole_AndSessionWorks()
    {
        var user = await _factory.SignUpAsync(AppRoles.Participant); // registers + logs in

        var me = await user.Client.GetAsync("/api/v1/users/me");

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var profile = await me.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.Equal(user.Email, profile!.Email);
        Assert.Equal(AppRoles.Participant, profile.Role);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var user = await _factory.SignUpAsync(AppRoles.Participant);

        var response = await _factory.NewClient().PostAsJsonAsync("/api/v1/auth/login",
            new { email = user.Email, password = "Wrong-Password-1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var response = await _factory.NewClient().PostAsJsonAsync("/api/v1/auth/login",
            new { email = NewEmail(), password = TestSupport.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_MissingFields_Returns400()
    {
        var response = await _factory.NewClient().PostAsJsonAsync("/api/v1/auth/login", new { email = "", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutLogin_Returns401()
    {
        var response = await _factory.NewClient().GetAsync("/api/v1/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsSession()
    {
        var user = await _factory.SignUpAsync(AppRoles.Participant);
        Assert.Equal(HttpStatusCode.OK, (await user.Client.GetAsync("/api/v1/users/me")).StatusCode);

        var logout = await user.Client.PostAsync("/api/v1/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await user.Client.GetAsync("/api/v1/users/me")).StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ChangesNameAndEmail_ButNotRole()
    {
        var user = await _factory.SignUpAsync(AppRoles.Participant);
        var newEmail = NewEmail();

        var response = await user.Client.PutAsJsonAsync("/api/v1/users/me", new { fullName = "New Name", email = newEmail });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.Equal("New Name", profile!.FullName);
        Assert.Equal(newEmail, profile.Email);
        Assert.Equal(AppRoles.Participant, profile.Role);
    }

    [Fact]
    public async Task UpdateProfile_EmailUsedByAnotherUser_Returns409()
    {
        var first = await _factory.SignUpAsync(AppRoles.Participant);
        var second = await _factory.SignUpAsync(AppRoles.Participant);

        var response = await second.Client.PutAsJsonAsync("/api/v1/users/me",
            new { fullName = "Second", email = first.Email });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
