using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "That does not look like a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your full name."), StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "That does not look like a valid email address."), StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a password."), StringLength(100, MinimumLength = 8, ErrorMessage = "Your password must be at least 8 characters long.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please repeat your password."), Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a role.")]
    public string Role { get; set; } = "Participant";

    [Display(Name = "Organiser invite code")]
    public string? InviteCode { get; set; }
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your full name."), StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "That does not look like a valid email address."), StringLength(150)]
    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public DateTime MemberSince { get; set; }
}
