using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class RegisterRequest
{
    /// <example>Lerato Molefe</example>
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <example>lerato@example.com</example>
    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    /// <summary>At least 8 characters.</summary>
    /// <example>Password123!</example>
    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>"Participant" (default) or "Organiser". Organiser accounts also need the invite code.</summary>
    /// <example>Participant</example>
    public string? Role { get; set; }

    /// <summary>Secret code issued by RaceDay admins. Only required when Role is "Organiser".</summary>
    public string? OrganiserInviteCode { get; set; }
}

public class LoginRequest
{
    /// <example>lerato.participant@raceday.co.za</example>
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <example>Password123!</example>
    [Required]
    public string Password { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    /// <example>Lerato M. Molefe</example>
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <example>lerato@example.com</example>
    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;
}

public class UserProfileDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    /// <example>Participant</example>
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
