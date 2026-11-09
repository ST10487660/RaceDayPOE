using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(Roles.Participant)]
public class ParticipantController : AppController
{
    private readonly ApiClient _api;

    public ParticipantController(ApiClient api) => _api = api;

    /// <summary>My Events: every event the participant has enrolled for, with the enrolment status.</summary>
    public async Task<IActionResult> MyEvents()
    {
        var result = await _api.GetMyEnrolmentsAsync();
        if (result.Status == 401) return SessionExpired();
        if (!result.Success) Failure(result.Message);
        return View(result.Data ?? new List<ApiEnrolment>());
    }

    /// <summary>My Results: race history for completed events (finish time and position out of total finishers).</summary>
    public async Task<IActionResult> MyResults()
    {
        var result = await _api.GetMyResultsAsync();
        if (result.Status == 401) return SessionExpired();
        if (!result.Success) Failure(result.Message);
        return View(result.Data ?? new List<ApiResultItem>());
    }

    [HttpPost]
    public async Task<IActionResult> Enrol(int eventId, int? categoryId)
    {
        if (categoryId == null)
        {
            Failure("Please choose a category before enrolling.");
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        var result = await _api.EnrolAsync(eventId, categoryId.Value);
        if (result.Status == 401) return SessionExpired();

        if (!result.Success)
        {
            Failure(result.Message);
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        Success("You are enrolled! Good luck on race day.");
        return RedirectToAction(nameof(MyEvents));
    }

    [HttpPost]
    public async Task<IActionResult> CancelEnrolment(int enrolmentId)
    {
        var result = await _api.CancelEnrolmentAsync(enrolmentId);
        if (result.Status == 401) return SessionExpired();

        if (result.Success) Success("Your enrolment has been cancelled.");
        else Failure(result.Message);

        return RedirectToAction(nameof(MyEvents));
    }
}
