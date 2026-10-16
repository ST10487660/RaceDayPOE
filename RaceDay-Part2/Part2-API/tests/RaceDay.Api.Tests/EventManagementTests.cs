using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests;

/// <summary>Event and category management by Organisers.</summary>
public class EventManagementTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EventManagementTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task Anyone_CanListEvents_WithoutLoggingIn()
    {
        var response = await _factory.NewClient().GetAsync("/api/v1/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCreateEvent_AndItAppearsInTheList()
    {
        var organiser = await _factory.SignUpAsync(AppRoles.Organiser);
        var category = await organiser.Client.CreateCategoryAsync();

        var created = await organiser.Client.CreateEventAsync(new[] { category.CategoryId });

        Assert.Equal(organiser.Profile.UserId, created.OrganiserId);
        Assert.Equal("Run", created.EventType);
        Assert.Single(created.Categories);

        var list = await _factory.NewClient().GetFromJsonAsync<List<EventDto>>("/api/v1/events");
        Assert.Contains(list!, e => e.EventId == created.EventId);
    }

    [Fact]
    public async Task Organiser_CreateEvent_WithInvalidData_Returns400()
    {
        var organiser = await _factory.SignUpAsync(AppRoles.Organiser);

        var response = await organiser.Client.PostAsJsonAsync("/api/v1/events", new
        {
            eventName = "",
            eventDate = "2027-01-01",
            location = "",
            eventType = "Run",
            distanceKm = 10,
            maxParticipants = 0,
            categoryIds = Array.Empty<int>()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CreateEvent_WithInvalidEventType_Returns400()
    {
        var organiser = await _factory.SignUpAsync(AppRoles.Organiser);

        var response = await organiser.Client.PostAsJsonAsync("/api/v1/events", new
        {
            eventName = "Swim Gala",
            eventDate = "2027-01-01",
            location = "Pool",
            eventType = "Swim",
            distanceKm = 1,
            maxParticipants = 10,
            categoryIds = Array.Empty<int>()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CreateEvent_WithUnknownCategory_Returns400()
    {
        var organiser = await _factory.SignUpAsync(AppRoles.Organiser);

        var response = await organiser.Client.PostAsJsonAsync("/api/v1/events",
            TestSupport.EventBody(new[] { 999999 }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent()
    {
        var organiser = await _factory.SignUpAsync(AppRoles.Organiser);
        var category = await organiser.Client.CreateCategoryAsync();
        var ev = await organiser.Client.CreateEventAsync(new[] { category.CategoryId });

        var response = await organiser.Client.PutAsJsonAsync($"/api/v1/events/{ev.EventId}",
            TestSupport.EventBody(new[] { category.CategoryId }, maxParticipants: 250, name: "Renamed Run"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EventDto>();
        Assert.Equal("Renamed Run", updated!.EventName);
        Assert.Equal(250, updated.MaxParticipants);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent_AndItIsGone()
    {
        var organiser = await _factory.SignUpAsync(AppRoles.Organiser);
        var category = await organiser.Client.CreateCategoryAsync();
        var ev = await organiser.Client.CreateEventAsync(new[] { category.CategoryId });

        var delete = await organiser.Client.DeleteAsync($"/api/v1/events/{ev.EventId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var get = await _factory.NewClient().GetAsync($"/api/v1/events/{ev.EventId}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotDeleteEventWithEnrolments_Returns409()
    {
        var organiser = await _factory.SignUpAsync(AppRoles.Organiser);
        var participant = await _factory.SignUpAsync(AppRoles.Participant);
        var category = await organiser.Client.CreateCategoryAsync();
        var ev = await organiser.Client.CreateEventAsync(new[] { category.CategoryId });
        await participant.Client.EnrolAsync(ev.EventId, category.CategoryId);

        var delete = await organiser.Client.DeleteAsync($"/api/v1/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotModifyAnotherOrganisersEvent_Returns403()
    {
        var owner = await _factory.SignUpAsync(AppRoles.Organiser);
        var other = await _factory.SignUpAsync(AppRoles.Organiser);
        var category = await owner.Client.CreateCategoryAsync();
        var ev = await owner.Client.CreateEventAsync(new[] { category.CategoryId });

        var update = await other.Client.PutAsJsonAsync($"/api/v1/events/{ev.EventId}",
            TestSupport.EventBody(new[] { category.CategoryId }));
        var delete = await other.Client.DeleteAsync($"/api/v1/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task UpdateOrDelete_UnknownEvent_Returns404()
    {
        var organiser = await _factory.SignUpAsync(AppRoles.Organiser);
        var category = await organiser.Client.CreateCategoryAsync();

        var update = await organiser.Client.PutAsJsonAsync("/api/v1/events/999999",
            TestSupport.EventBody(new[] { category.CategoryId }));
        var delete = await organiser.Client.DeleteAsync("/api/v1/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task GetEvent_Unknown_Returns404WithErrorBody()
    {
        var response = await _factory.NewClient().GetAsync("/api/v1/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal(404, error!.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_CreateEvent_Returns401()
    {
        var response = await _factory.NewClient().PostAsJsonAsync("/api/v1/events",
            TestSupport.EventBody(Array.Empty<int>()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanManageCategories_AndBothRolesCanViewThem()
    {
        var organiser = await _factory.SignUpAsync(AppRoles.Organiser);
        var participant = await _factory.SignUpAsync(AppRoles.Participant);

        var category = await organiser.Client.CreateCategoryAsync();

        var update = await organiser.Client.PutAsJsonAsync($"/api/v1/categories/{category.CategoryId}",
            new { categoryName = category.CategoryName + " (renamed)", description = "Updated" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var seenByParticipant = await participant.Client.GetFromJsonAsync<List<CategoryDto>>("/api/v1/categories");
        Assert.Contains(seenByParticipant!, c => c.CategoryId == category.CategoryId);

        var delete = await organiser.Client.DeleteAsync($"/api/v1/categories/{category.CategoryId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_DuplicateName_Returns409()
    {
        var organiser = await _factory.SignUpAsync(AppRoles.Organiser);
        var category = await organiser.Client.CreateCategoryAsync();

        var response = await organiser.Client.PostAsJsonAsync("/api/v1/categories",
            new { categoryName = category.CategoryName.ToUpperInvariant(), description = "dup" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_InUse_Returns409()
    {
        var organiser = await _factory.SignUpAsync(AppRoles.Organiser);
        var category = await organiser.Client.CreateCategoryAsync();
        await organiser.Client.CreateEventAsync(new[] { category.CategoryId });

        var response = await organiser.Client.DeleteAsync($"/api/v1/categories/{category.CategoryId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
