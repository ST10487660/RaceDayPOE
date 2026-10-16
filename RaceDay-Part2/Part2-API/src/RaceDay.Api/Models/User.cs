namespace RaceDay.Api.Models;

public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    /// <summary>PBKDF2 hash from ASP.NET Core's PasswordHasher. The plain password is never stored.</summary>
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = AppRoles.Participant;
    public DateTime CreatedAt { get; set; }

    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
