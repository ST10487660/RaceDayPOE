using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Infrastructure;

/// <summary>
/// Guards a controller or action using the session: not logged in -> redirected to the login page;
/// logged in with the wrong role -> redirected to the "access denied" page.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _role;

    public SessionAuthorizeAttribute(string? role = null) => _role = role;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();

        if (!user.IsAuthenticated)
        {
            var request = context.HttpContext.Request;
            var returnUrl = request.Method == HttpMethods.Get ? request.Path + request.QueryString : null;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl, message = "login" });
            return;
        }

        if (_role != null && user.Role != _role)
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }
}
