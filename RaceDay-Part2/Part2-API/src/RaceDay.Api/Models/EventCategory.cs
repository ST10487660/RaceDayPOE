namespace RaceDay.Api.Models;

/// <summary>Join entity: which categories are offered at which event.</summary>
public class EventCategory
{
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
