using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests;

/// <summary>Capturing and viewing results.</summary>
public class ResultsTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public ResultsTests(RaceDayFactory factory) => _factory = factory;

    private async Task<(TestUser Organiser, TestUser Participant, EventDto Event, EnrolmentDto Enrolment)> SetUpAsync()
    {
        var organiser = await _factory.SignUpAsync(AppRoles.Organiser);
        var participant = await _factory.SignUpAsync(AppRoles.Participant);
        var category = await organiser.Client.CreateCategoryAsync();
        var ev = await organiser.Client.CreateEventAsync(new[] { category.CategoryId });
        var enrolment = await participant.Client.EnrolAsync(ev.EventId, category.CategoryId);
        return (organiser, participant, ev, enrolment);
    }

    [Fact]
    public async Task Organiser_CanCaptureResult_AndParticipantCanViewIt()
    {
        var (organiser, participant, ev, enrolment) = await SetUpAsync();

        var capture = await organiser.Client.PostAsJsonAsync($"/api/v1/events/{ev.EventId}/results",
            new { enrolmentId = enrolment.EnrolmentId, position = 3, finishTime = "00:45:30", distanceKm = 10 });

        Assert.Equal(HttpStatusCode.Created, capture.StatusCode);

        var mine = await participant.Client.GetFromJsonAsync<List<ResultDto>>("/api/v1/results/me");
        var result = Assert.Single(mine!, r => r.EnrolmentId == enrolment.EnrolmentId);
        Assert.Equal(3, result.Position);
        Assert.Equal(new TimeSpan(0, 45, 30), result.FinishTime);

        var forOrganiser = await organiser.Client.GetFromJsonAsync<List<ResultDto>>($"/api/v1/events/{ev.EventId}/results");
        Assert.Contains(forOrganiser!, r => r.ResultId == result.ResultId);
    }

    [Fact]
    public async Task Participant_OnlySeesOwnResults()
    {
        var (organiser, _, ev, enrolment) = await SetUpAsync();
        await organiser.Client.PostAsJsonAsync($"/api/v1/events/{ev.EventId}/results",
            new { enrolmentId = enrolment.EnrolmentId, position = 1, finishTime = "00:40:00" });

        var otherParticipant = await _factory.SignUpAsync(AppRoles.Participant);
        var theirs = await otherParticipant.Client.GetFromJsonAsync<List<ResultDto>>("/api/v1/results/me");

        Assert.Empty(theirs!);
    }

    [Fact]
    public async Task CaptureResult_Twice_Returns409()
    {
        var (organiser, _, ev, enrolment) = await SetUpAsync();
        var body = new { enrolmentId = enrolment.EnrolmentId, position = 1, finishTime = "00:40:00" };

        Assert.Equal(HttpStatusCode.Created, (await organiser.Client.PostAsJsonAsync($"/api/v1/events/{ev.EventId}/results", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await organiser.Client.PostAsJsonAsync($"/api/v1/events/{ev.EventId}/results", body)).StatusCode);
    }

    [Theory]
    [InlineData(0, "00:40:00")]       // position must be positive
    [InlineData(1, "00:00:00")]       // finish time must be positive
    public async Task CaptureResult_InvalidData_Returns400(int position, string finishTime)
    {
        var (organiser, _, ev, enrolment) = await SetUpAsync();

        var response = await organiser.Client.PostAsJsonAsync($"/api/v1/events/{ev.EventId}/results",
            new { enrolmentId = enrolment.EnrolmentId, position, finishTime });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ForEnrolmentFromAnotherEvent_Returns404()
    {
        var (organiser, _, ev, _) = await SetUpAsync();
        var (_, _, _, foreignEnrolment) = await SetUpAsync();

        var response = await organiser.Client.PostAsJsonAsync($"/api/v1/events/{ev.EventId}/results",
            new { enrolmentId = foreignEnrolment.EnrolmentId, position = 1, finishTime = "00:40:00" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_CannotCaptureOrViewResults_Returns403()
    {
        var (_, _, ev, enrolment) = await SetUpAsync();
        var stranger = await _factory.SignUpAsync(AppRoles.Organiser);

        var capture = await stranger.Client.PostAsJsonAsync($"/api/v1/events/{ev.EventId}/results",
            new { enrolmentId = enrolment.EnrolmentId, position = 1, finishTime = "00:40:00" });
        var view = await stranger.Client.GetAsync($"/api/v1/events/{ev.EventId}/results");

        Assert.Equal(HttpStatusCode.Forbidden, capture.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, view.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCorrectAndDeleteResult()
    {
        var (organiser, _, ev, enrolment) = await SetUpAsync();
        var created = await (await organiser.Client.PostAsJsonAsync($"/api/v1/events/{ev.EventId}/results",
            new { enrolmentId = enrolment.EnrolmentId, position = 5, finishTime = "00:50:00" }))
            .Content.ReadFromJsonAsync<ResultDto>();

        var update = await organiser.Client.PutAsJsonAsync($"/api/v1/results/{created!.ResultId}",
            new { position = 2, finishTime = "00:44:10" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<ResultDto>();
        Assert.Equal(2, updated!.Position);

        var delete = await organiser.Client.DeleteAsync($"/api/v1/results/{created.ResultId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await organiser.Client.DeleteAsync($"/api/v1/results/{created.ResultId}")).StatusCode);
    }
}
