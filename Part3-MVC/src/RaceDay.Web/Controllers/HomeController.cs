using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : AppController
{
    private readonly ApiClient _api;

    public HomeController(ApiClient api) => _api = api;

    /// <summary>Home page: all upcoming events with search and filter options.</summary>
    public async Task<IActionResult> Index(string? search, string? type, DateOnly? from, DateOnly? to)
    {
        var model = new HomeIndexViewModel { Search = search, EventType = type, From = from, To = to };

        var result = await _api.GetEventsAsync(search, type, from, to);
        if (result.Success)
            model.Events = result.Data ?? new();
        else
            Failure(result.Message);

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();

    public IActionResult HttpStatus(int code)
    {
        Response.StatusCode = code;
        return View(code);
    }
}
