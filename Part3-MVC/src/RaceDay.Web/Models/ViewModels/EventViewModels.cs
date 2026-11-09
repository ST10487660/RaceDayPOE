using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models.ViewModels;

public class HomeIndexViewModel
{
    public List<ApiEvent> Events { get; set; } = new();
    public string? Search { get; set; }
    public string? EventType { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public bool HasFilters => !string.IsNullOrWhiteSpace(Search) || !string.IsNullOrWhiteSpace(EventType) || From.HasValue || To.HasValue;
}

public class EventDetailsViewModel
{
    public ApiEvent Event { get; set; } = new();
    public ApiWeather? Weather { get; set; }
    /// <summary>The logged-in participant's enrolment in this event, if any.</summary>
    public ApiEnrolment? MyEnrolment { get; set; }
}

public class EventFormViewModel
{
    public int? EventId { get; set; }

    [Required(ErrorMessage = "Please enter the event name."), StringLength(150)]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "The description can be at most 500 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose the event date.")]
    [Display(Name = "Date")]
    public DateOnly? EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location."), StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose the event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    [Range(0.01, 1000, ErrorMessage = "Distance must be between 0.01 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    [Range(1, 1000000, ErrorMessage = "Maximum participants must be at least 1.")]
    [Display(Name = "Maximum participants")]
    public int MaxParticipants { get; set; } = 100;

    public List<int> SelectedCategoryIds { get; set; } = new();

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }

    public string? ExistingBannerUrl { get; set; }

    [ValidateNever]
    public List<ApiCategory> AvailableCategories { get; set; } = new();
}

public class DashboardViewModel
{
    public List<ApiEvent> Events { get; set; } = new();
    public int TotalEvents => Events.Count;
    public int UpcomingEvents { get; set; }
    public int TotalEnrolments => Events.Sum(e => e.EnrolledCount);
    public ApiEvent? NextEvent { get; set; }
}

public class CategoryFormViewModel
{
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Please enter a category name."), StringLength(100)]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "The description can be at most 300 characters.")]
    public string? Description { get; set; }
}

public class CategoriesPageViewModel
{
    public List<ApiCategory> Categories { get; set; } = new();
    public CategoryFormViewModel NewCategory { get; set; } = new();
}

public class ResultRow
{
    public ApiEnrolment Enrolment { get; set; } = new();
    public ApiResultItem? Result { get; set; }
}

public class ResultsPageViewModel
{
    public ApiEvent Event { get; set; } = new();
    public List<ResultRow> Rows { get; set; } = new();
    public bool Concluded { get; set; }
}
