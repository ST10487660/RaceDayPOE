using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests;

/// <summary>Event enrolments for Participants, and enrolment visibility for Organisers.</summary>
public class EnrolmentTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EnrolmentTests(RaceDayFactory factory) => _factory = factory;

    private async Task<(TestUser Organiser, CategoryDto Category, EventDto Event)> SetUpEventAsync(int maxParticipants = 100)
    {
        var organiser = await _factory.SignUpAsync(AppRoles.Organiser);
        var category = await organiser.Client.CreateCategoryAsync();
        var ev = await organiser.Client.CreateEventAsync(new[] { category.CategoryId }, maxParticipants);
        return (organiser, category, ev);
    }

    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsCorrectlyRecorded()
    {
        var (_, category, ev) = await SetUpEventAsync();
        var participant = await _factory.SignUpAsync(AppRoles.Participant);

        var response = await participant.Client.PostAsJsonAsync($"/api/v1/events/{ev.EventId}/enrolments",
            new { categoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<EnrolmentDto>())!;
        Assert.Equal(participant.Profile.UserId, created.ParticipantId);
        Assert.Equal(ev.EventId, created.EventId);
        Assert.Equal(category.CategoryId, created.CategoryId);
        Assert.Equal(EnrolmentStatuses.Confirmed, created.Status);

        // Visible through the participant's own enrolments...
        var mine = await participant.Client.GetFromJsonAsync<List<EnrolmentDto>>("/api/v1/enrolments/me");
        Assert.Contains(mine!, e => e.EnrolmentId == created.EnrolmentId
                                    && e.EventId == ev.EventId
                                    && e.CategoryId == category.CategoryId);

        // ...and really stored in the database with the right link between participant, event and category.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var row = await db.Enrolments.SingleAsync(e => e.EnrolmentId == created.EnrolmentId);
        Assert.Equal(participant.Profile.UserId, row.ParticipantId);
        Assert.Equal(ev.EventId, row.EventId);
        Assert.Equal(category.CategoryId, row.CategoryId);
    }

    [Fact]
    public async Task Enrol_Twice_Returns409()
    {
        var (_, category, ev) = await SetUpEventAsync();
        var participant = await _factory.SignUpAsync(AppRoles.Participant);
        await participant.Client.EnrolAsync(ev.EventId, category.CategoryId);

        var second = await participant.Client.PostAsJsonAsync($"/api/v1/events/{ev.EventId}/enrolments",
            new { categoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithCategoryNotOfferedAtEvent_Returns400()
    {
        var (organiser, _, ev) = await SetUpEventAsync();
        var unrelated = await organiser.Client.CreateCategoryAsync();
        var participant = await _factory.SignUpAsync(AppRoles.Participant);

        var response = await participant.Client.PostAsJsonAsync($"/api/v1/events/{ev.EventId}/enrolments",
            new { categoryId = unrelated.CategoryId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InUnknownEvent_Returns404()
    {
        var participant = await _factory.SignUpAsync(AppRoles.Participant);

        var response = await participant.Client.PostAsJsonAsync("/api/v1/events/999999/enrolments", new { categoryId = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WhenEventIsFull_Returns409()
    {
        var (_, category, ev) = await SetUpEventAsync(maxParticipants: 1);
        var first = await _factory.SignUpAsync(AppRoles.Participant);
        var second = await _factory.SignUpAsync(AppRoles.Participant);
        await first.Client.EnrolAsync(ev.EventId, category.CategoryId);

        var response = await second.Client.PostAsJsonAsync($"/api/v1/events/{ev.EventId}/enrolments",
            new { categoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewEnrolmentsForOwnEvent_ButNotAnotherOrganisersEvent()
    {
        var (organiser, category, ev) = await SetUpEventAsync();
        var participant = await _factory.SignUpAsync(AppRoles.Participant);
        var enrolment = await participant.Client.EnrolAsync(ev.EventId, category.CategoryId);

        var own = await organiser.Client.GetFromJsonAsync<List<EnrolmentDto>>($"/api/v1/events/{ev.EventId}/enrolments");
        Assert.Contains(own!, e => e.EnrolmentId == enrolment.EnrolmentId);

        var stranger = await _factory.SignUpAsync(AppRoles.Organiser);
        var forbidden = await stranger.Client.GetAsync($"/api/v1/events/{ev.EventId}/enrolments");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task Participant_OnlySeesOwnEnrolments()
    {
        var (_, category, ev) = await SetUpEventAsync();
        var alice = await _factory.SignUpAsync(AppRoles.Participant);
        var bob = await _factory.SignUpAsync(AppRoles.Participant);
        var aliceEnrolment = await alice.Client.EnrolAsync(ev.EventId, category.CategoryId);

        var bobsList = await bob.Client.GetFromJsonAsync<List<EnrolmentDto>>("/api/v1/enrolments/me");

        Assert.DoesNotContain(bobsList!, e => e.EnrolmentId == aliceEnrolment.EnrolmentId);
    }

    [Fact]
    public async Task Participant_CanCancelOwnEnrolment_AndReEnrol()
    {
        var (_, category, ev) = await SetUpEventAsync(maxParticipants: 1);
        var participant = await _factory.SignUpAsync(AppRoles.Participant);
        var enrolment = await participant.Client.EnrolAsync(ev.EventId, category.CategoryId);

        var cancel = await participant.Client.DeleteAsync($"/api/v1/enrolments/{enrolment.EnrolmentId}");
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

        var mine = await participant.Client.GetFromJsonAsync<List<EnrolmentDto>>("/api/v1/enrolments/me");
        Assert.Equal(EnrolmentStatuses.Cancelled, mine!.Single(e => e.EnrolmentId == enrolment.EnrolmentId).Status);

        // The freed spot can be taken again (event capacity is 1).
        var again = await participant.Client.PostAsJsonAsync($"/api/v1/events/{ev.EventId}/enrolments",
            new { categoryId = category.CategoryId });
        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCancelSomeoneElsesEnrolment_Returns403()
    {
        var (_, category, ev) = await SetUpEventAsync();
        var owner = await _factory.SignUpAsync(AppRoles.Participant);
        var other = await _factory.SignUpAsync(AppRoles.Participant);
        var enrolment = await owner.Client.EnrolAsync(ev.EventId, category.CategoryId);

        var response = await other.Client.DeleteAsync($"/api/v1/enrolments/{enrolment.EnrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_UnknownEnrolment_Returns404()
    {
        var participant = await _factory.SignUpAsync(AppRoles.Participant);

        var response = await participant.Client.DeleteAsync("/api/v1/enrolments/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
