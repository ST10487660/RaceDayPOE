namespace RaceDay.Api.Models;

/// <summary>Maps to the "Results" table.</summary>
public class RaceResult
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public Enrolment Enrolment { get; set; } = null!;
    public int? Position { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public decimal? DistanceKm { get; set; }
    public DateTime RecordedAt { get; set; }
}
