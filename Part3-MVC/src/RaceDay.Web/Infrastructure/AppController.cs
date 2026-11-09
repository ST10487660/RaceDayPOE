using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Infrastructure;

/// <summary>Shared helpers for all controllers: flash messages, API error display, expired-session handling.</summary>
public abstract class AppController : Controller
{
    protected ICurrentUser CurrentUser => HttpContext.RequestServices.GetRequiredService<ICurrentUser>();

    protected void Success(string message) => TempData["Success"] = message;

    protected void Failure(string message) => TempData["Error"] = message;

    /// <summary>The API no longer recognises our session (expired): clear it and send the user to log in again.</summary>
    protected IActionResult SessionExpired()
    {
        CurrentUser.SignOut();
        Failure("Your session has expired. Please log in again.");
        return RedirectToAction("Login", "Account");
    }

    /// <summary>Shows the API's friendly validation messages next to the form (never raw errors).</summary>
    protected void AddApiErrors<T>(ApiResult<T> result)
    {
        foreach (var (key, messages) in result.Errors)
            foreach (var message in messages)
                ModelState.AddModelError(key.StartsWith('$') ? string.Empty : key, key.StartsWith('$') ? "Please check the values you entered." : message);

        if (result.Errors.Count == 0)
            ModelState.AddModelError(string.Empty, result.Message);
    }
}
