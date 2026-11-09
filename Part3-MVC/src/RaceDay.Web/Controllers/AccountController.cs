using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : AppController
{
    private readonly ApiClient _api;

    public AccountController(ApiClient api) => _api = api;

    [HttpGet]
    public IActionResult Register()
    {
        if (CurrentUser.IsAuthenticated) return RedirectToHome();
        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _api.RegisterAsync(model.FullName, model.Email, model.Password, model.Role, model.InviteCode);
        if (!result.Success)
        {
            AddApiErrors(result);
            return View(model);
        }

        Success("Your account has been created. Please log in.");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl, string? message)
    {
        if (CurrentUser.IsAuthenticated) return RedirectToHome();
        if (message == "login") TempData["Error"] ??= "Please log in to view that page.";
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        CurrentUser.SignOut(); // always start from a clean session
        var result = await _api.LoginAsync(model.Email, model.Password);
        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.Status == 401
                ? "That email and password do not match. Please try again."
                : result.Message);
            return View(model);
        }

        CurrentUser.SignIn(result.Data);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl) && !CurrentUser.IsOrganiser)
            return LocalRedirect(model.ReturnUrl);

        return RedirectToHome();
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        if (CurrentUser.IsAuthenticated)
            await _api.LogoutAsync(); // ends the API session too

        CurrentUser.SignOut();       // clears the MVC session (user id, role, API cookie)
        Success("You have been logged out.");
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    private IActionResult RedirectToHome() =>
        CurrentUser.IsOrganiser ? RedirectToAction("Dashboard", "Organiser") : RedirectToAction("Index", "Home");
}
