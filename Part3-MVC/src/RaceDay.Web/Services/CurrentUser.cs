using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Services;

/// <summary>Who is logged in right now, read from (and written to) the server-side session.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    int UserId { get; }
    string Role { get; }
    string FullName { get; }
    string? ProfilePictureUrl { get; }
    bool IsOrganiser { get; }
    bool IsParticipant { get; }
    void SignIn(ApiUser user);
    void Refresh(ApiUser user);
    void SignOut();
}

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ISession? Session => _accessor.HttpContext?.Session;

    public bool IsAuthenticated => UserId > 0 && !string.IsNullOrEmpty(Role);
    public int UserId => Session?.GetInt32(SessionKeys.UserId) ?? 0;
    public string Role => Session?.GetString(SessionKeys.Role) ?? string.Empty;
    public string FullName => Session?.GetString(SessionKeys.FullName) ?? string.Empty;
    public string? ProfilePictureUrl => Session?.GetString(SessionKeys.ProfilePictureUrl);
    public bool IsOrganiser => Role == Roles.Organiser;
    public bool IsParticipant => Role == Roles.Participant;

    public void SignIn(ApiUser user) => Refresh(user);

    public void Refresh(ApiUser user)
    {
        var session = Session!;
        session.SetInt32(SessionKeys.UserId, user.UserId);
        session.SetString(SessionKeys.Role, user.Role);
        session.SetString(SessionKeys.FullName, user.FullName);
        session.SetString(SessionKeys.Email, user.Email);
        session.SetString(SessionKeys.ProfilePictureUrl, user.ProfilePictureUrl ?? string.Empty);
    }

    public void SignOut() => Session?.Clear();
}
