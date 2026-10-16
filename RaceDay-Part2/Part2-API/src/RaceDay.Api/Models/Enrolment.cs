namespace RaceDay.Api.Models;

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    /// <summary>Composite FK (EventId, CategoryId) guarantees the category really belongs to the event.</summary>
    public EventCategory EventCategory { get; set; } = null!;
    public int ParticipantId { get; set; }
    public User Participant { get; set; } = null!;
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = EnrolmentStatuses.Confirmed;

    public RaceResult? Result { get; set; }
}
