namespace RaceDay.Web.Services;

public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
    public const string FullName = "FullName";
    public const string Email = "Email";
    public const string ProfilePictureUrl = "ProfilePictureUrl";
    /// <summary>The API's own session cookie (name=value), forwarded on every API call.</summary>
    public const string ApiCookie = "ApiCookie";
}
