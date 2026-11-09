using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize]
public class ProfileController : AppController
{
    private readonly ApiClient _api;

    public ProfileController(ApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetMeAsync();
        if (result.Status == 401) return SessionExpired();
        if (!result.Success || result.Data == null)
        {
            Failure(result.Message);
            return RedirectToAction("Index", "Home");
        }

        CurrentUser.Refresh(result.Data);
        return View(ToViewModel(result.Data));
    }

    [HttpPost]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await FillReadOnlyFieldsAsync(model);
            return View(nameof(Index), model);
        }

        var result = await _api.UpdateMeAsync(model.FullName, model.Email);
        if (result.Status == 401) return SessionExpired();

        if (!result.Success)
        {
            AddApiErrors(result);
            await FillReadOnlyFieldsAsync(model);
            return View(nameof(Index), model);
        }

        CurrentUser.Refresh(result.Data!);
        Success("Your profile has been updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture == null || picture.Length == 0)
        {
            Failure("Please choose an image to upload.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.UploadPictureAsync(picture);
        if (result.Status == 401) return SessionExpired();

        if (result.Success)
        {
            CurrentUser.Refresh(result.Data!);
            Success("Your profile picture has been updated.");
        }
        else
        {
            Failure(result.Message);
        }

        return RedirectToAction(nameof(Index));
    }

    private static ProfileViewModel ToViewModel(ApiUser user) => new()
    {
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role,
        ProfilePictureUrl = user.ProfilePictureUrl,
        MemberSince = user.CreatedAt
    };

    private async Task FillReadOnlyFieldsAsync(ProfileViewModel model)
    {
        model.Role = CurrentUser.Role;
        var me = await _api.GetMeAsync();
        if (me.Success && me.Data != null)
        {
            model.ProfilePictureUrl = me.Data.ProfilePictureUrl;
            model.MemberSince = me.Data.CreatedAt;
        }
    }
}
