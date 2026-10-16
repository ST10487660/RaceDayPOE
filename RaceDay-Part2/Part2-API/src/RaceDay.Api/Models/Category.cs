namespace RaceDay.Api.Models;

public class Category
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();
}
