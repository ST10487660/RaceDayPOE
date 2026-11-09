using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class EventsController : AppController
{
    private readonly ApiClient _api;

    public EventsController(ApiClient api) => _api = api;

    /// <summary>Full event information, categories, banner, route, weather and (for participants) the enrol form.</summary>
    public async Task<IActionResult> Details(int id)
    {
        var eventResult = await _api.GetEventAsync(id);
        if (eventResult.Status == 404) return NotFound();
        if (!eventResult.Success)
        {
            Failure(eventResult.Message);
            return RedirectToAction("Index", "Home");
        }

        var model = new EventDetailsViewModel { Event = eventResult.Data! };

        var weather = await _api.GetWeatherAsync(id);
        if (weather.Success) model.Weather = weather.Data;

        if (CurrentUser.IsParticipant)
        {
            var mine = await _api.GetMyEnrolmentsAsync();
            if (mine.Status == 401) return SessionExpired();
            model.MyEnrolment = mine.Data?.FirstOrDefault(e => e.EventId == id && e.Status != "Cancelled");
        }

        return View(model);
    }
}
