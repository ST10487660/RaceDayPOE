using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Everything an Organiser can do: dashboard, events, categories, enrolments and results.</summary>
[SessionAuthorize(Roles.Organiser)]
public class OrganiserController : AppController
{
    private readonly ApiClient _api;

    public OrganiserController(ApiClient api) => _api = api;

    // ------------------------------------------------------------------ dashboard

    public async Task<IActionResult> Dashboard()
    {
        var result = await _api.GetMyEventsAsync();
        if (result.Status == 401) return SessionExpired();
        if (!result.Success) Failure(result.Message);

        var events = result.Data ?? new List<ApiEvent>();
        var today = ViewHelpers.Today;
        var model = new DashboardViewModel
        {
            Events = events.OrderBy(e => e.EventDate).ToList(),
            UpcomingEvents = events.Count(e => e.EventDate >= today),
            NextEvent = events.Where(e => e.EventDate >= today).OrderBy(e => e.EventDate).FirstOrDefault()
        };
        return View(model);
    }

    // ------------------------------------------------------------------ events

    public async Task<IActionResult> Events()
    {
        var result = await _api.GetMyEventsAsync();
        if (result.Status == 401) return SessionExpired();
        if (!result.Success) Failure(result.Message);
        return View(result.Data ?? new List<ApiEvent>());
    }

    [HttpGet]
    public async Task<IActionResult> CreateEvent()
    {
        var model = new EventFormViewModel();
        return await LoadCategoriesAsync(model) ?? View("EventForm", model);
    }

    [HttpPost]
    public Task<IActionResult> CreateEvent(EventFormViewModel model) => SaveEventAsync(model, null);

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var result = await _api.GetEventAsync(id);
        if (result.Status == 401) return SessionExpired();
        if (result.Status == 404) return NotFound();
        if (!result.Success)
        {
            Failure(result.Message);
            return RedirectToAction(nameof(Events));
        }

        var ev = result.Data!;
        if (ev.OrganiserId != CurrentUser.UserId)
        {
            Failure("You can only edit your own events.");
            return RedirectToAction(nameof(Events));
        }

        var model = new EventFormViewModel
        {
            EventId = ev.EventId,
            EventName = ev.EventName,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            EventType = ev.EventType,
            DistanceKm = ev.DistanceKm,
            MaxParticipants = ev.MaxParticipants,
            SelectedCategoryIds = ev.Categories.Select(c => c.CategoryId).ToList(),
            ExistingBannerUrl = ev.BannerImageUrl
        };
        return await LoadCategoriesAsync(model) ?? View("EventForm", model);
    }

    [HttpPost]
    public Task<IActionResult> EditEvent(int id, EventFormViewModel model) => SaveEventAsync(model, id);

    [HttpPost]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var result = await _api.DeleteEventAsync(id);
        if (result.Status == 401) return SessionExpired();

        if (result.Success) Success("The event has been deleted.");
        else Failure(result.Message);

        return RedirectToAction(nameof(Events));
    }

    private async Task<IActionResult> SaveEventAsync(EventFormViewModel model, int? id)
    {
        model.EventId = id;
        var expired = await LoadCategoriesAsync(model);
        if (expired != null) return expired;

        if (!ModelState.IsValid) return View("EventForm", model);

        var request = new ApiEventRequest
        {
            EventName = model.EventName,
            Description = model.Description,
            EventDate = model.EventDate,
            Location = model.Location,
            EventType = model.EventType,
            DistanceKm = model.DistanceKm,
            MaxParticipants = model.MaxParticipants,
            CategoryIds = model.SelectedCategoryIds
        };

        var result = id == null ? await _api.CreateEventAsync(request) : await _api.UpdateEventAsync(id.Value, request);
        if (result.Status == 401) return SessionExpired();
        if (!result.Success)
        {
            AddApiErrors(result);
            return View("EventForm", model);
        }

        var saved = result.Data!;
        if (model.Banner is { Length: > 0 })
        {
            var upload = await _api.UploadBannerAsync(saved.EventId, model.Banner);
            if (upload.Status == 401) return SessionExpired();
            if (!upload.Success)
            {
                Failure($"The event was saved, but the banner could not be uploaded: {upload.Message}");
                return RedirectToAction(nameof(EditEvent), new { id = saved.EventId });
            }
        }

        Success(id == null ? "Your event has been created." : "Your event has been updated.");
        return RedirectToAction(nameof(Events));
    }

    /// <summary>Loads the category checkboxes. Returns a redirect only if the session has expired.</summary>
    private async Task<IActionResult?> LoadCategoriesAsync(EventFormViewModel model)
    {
        var categories = await _api.GetCategoriesAsync();
        if (categories.Status == 401) return SessionExpired();
        if (!categories.Success) Failure(categories.Message);
        model.AvailableCategories = categories.Data ?? new List<ApiCategory>();
        return null;
    }

    // ------------------------------------------------------------------ categories

    [HttpGet]
    public async Task<IActionResult> Categories()
    {
        var result = await _api.GetCategoriesAsync();
        if (result.Status == 401) return SessionExpired();
        if (!result.Success) Failure(result.Message);
        return View(new CategoriesPageViewModel { Categories = result.Data ?? new List<ApiCategory>() });
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory([Bind(Prefix = "NewCategory")] CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var list = await _api.GetCategoriesAsync();
            if (list.Status == 401) return SessionExpired();
            return View(nameof(Categories), new CategoriesPageViewModel { Categories = list.Data ?? new(), NewCategory = model });
        }

        var result = await _api.CreateCategoryAsync(model.CategoryName, model.Description);
        if (result.Status == 401) return SessionExpired();

        if (result.Success) Success($"Category \"{model.CategoryName}\" has been added.");
        else Failure(result.Message);

        return RedirectToAction(nameof(Categories));
    }

    [HttpGet]
    public async Task<IActionResult> EditCategory(int id)
    {
        var result = await _api.GetCategoriesAsync();
        if (result.Status == 401) return SessionExpired();

        var category = result.Data?.FirstOrDefault(c => c.CategoryId == id);
        if (category == null) return NotFound();

        return View(new CategoryFormViewModel { CategoryId = category.CategoryId, CategoryName = category.CategoryName, Description = category.Description });
    }

    [HttpPost]
    public async Task<IActionResult> EditCategory(int id, CategoryFormViewModel model)
    {
        model.CategoryId = id;
        if (!ModelState.IsValid) return View(model);

        var result = await _api.UpdateCategoryAsync(id, model.CategoryName, model.Description);
        if (result.Status == 401) return SessionExpired();
        if (!result.Success)
        {
            AddApiErrors(result);
            return View(model);
        }

        Success("The category has been updated.");
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var result = await _api.DeleteCategoryAsync(id);
        if (result.Status == 401) return SessionExpired();

        if (result.Success) Success("The category has been deleted.");
        else Failure(result.Message);

        return RedirectToAction(nameof(Categories));
    }

    // ------------------------------------------------------------------ enrolments

    public async Task<IActionResult> Enrolments(int id)
    {
        var ev = await _api.GetEventAsync(id);
        if (ev.Status == 401) return SessionExpired();
        if (ev.Status == 404) return NotFound();
        if (!ev.Success)
        {
            Failure(ev.Message);
            return RedirectToAction(nameof(Events));
        }

        var enrolments = await _api.GetEventEnrolmentsAsync(id);
        if (enrolments.Status == 401) return SessionExpired();
        if (!enrolments.Success)
        {
            Failure(enrolments.Message);
            return RedirectToAction(nameof(Events));
        }

        ViewBag.Event = ev.Data;
        return View(enrolments.Data ?? new List<ApiEnrolment>());
    }

    // ------------------------------------------------------------------ results

    [HttpGet]
    public async Task<IActionResult> Results(int id)
    {
        var ev = await _api.GetEventAsync(id);
        if (ev.Status == 401) return SessionExpired();
        if (ev.Status == 404) return NotFound();
        if (!ev.Success)
        {
            Failure(ev.Message);
            return RedirectToAction(nameof(Events));
        }

        var enrolments = await _api.GetEventEnrolmentsAsync(id);
        if (enrolments.Status == 401) return SessionExpired();
        if (!enrolments.Success)
        {
            Failure(enrolments.Message);
            return RedirectToAction(nameof(Events));
        }

        var results = await _api.GetEventResultsAsync(id);
        var byEnrolment = (results.Data ?? new List<ApiResultItem>()).ToDictionary(r => r.EnrolmentId);

        var model = new ResultsPageViewModel
        {
            Event = ev.Data!,
            Concluded = ev.Data!.EventDate <= ViewHelpers.Today,
            Rows = (enrolments.Data ?? new List<ApiEnrolment>())
                .Where(e => e.Status != "Cancelled")
                .Select(e => new ResultRow { Enrolment = e, Result = byEnrolment.GetValueOrDefault(e.EnrolmentId) })
                .OrderBy(r => r.Result?.Position ?? int.MaxValue)
                .ThenBy(r => r.Enrolment.ParticipantName)
                .ToList()
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> SaveResult(int id, int enrolmentId, int? resultId, int? position, string? finishTime, decimal? distanceKm)
    {
        if (position is null or < 1)
        {
            Failure("Please enter the finishing position (1 or higher).");
            return RedirectToAction(nameof(Results), new { id });
        }

        if (!TimeSpan.TryParseExact(finishTime?.Trim(), @"h\:mm\:ss", System.Globalization.CultureInfo.InvariantCulture, out var time))
        {
            Failure("Please enter the finish time as hh:mm:ss, for example 00:52:14.");
            return RedirectToAction(nameof(Results), new { id });
        }

        var result = resultId.HasValue
            ? await _api.UpdateResultAsync(resultId.Value, position.Value, time, distanceKm)
            : await _api.CaptureResultAsync(id, enrolmentId, position.Value, time, distanceKm);

        if (result.Status == 401) return SessionExpired();

        if (result.Success) Success("The result has been saved.");
        else Failure(result.Message);

        return RedirectToAction(nameof(Results), new { id });
    }
}
