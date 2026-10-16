using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class EnrolmentRequest
{
    /// <summary>The category the participant wants to enter. Must be offered at the event.</summary>
    /// <example>1</example>
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}

public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    /// <example>Confirmed</example>
    public string Status { get; set; } = string.Empty;
}

/// <summary>Fields for correcting a result.</summary>
public class ResultRequest
{
    /// <example>18</example>
    [Required, Range(1, 1000000)]
    public int? Position { get; set; }

    /// <summary>Finish time as hh:mm:ss.</summary>
    /// <example>00:52:14</example>
    [Required]
    public TimeSpan? FinishTime { get; set; }

    /// <example>10</example>
    [Range(0.01, 1000)]
    public decimal? DistanceKm { get; set; }
}

public class CreateResultRequest : ResultRequest
{
    /// <summary>The enrolment (participant + category) this result belongs to.</summary>
    /// <example>1</example>
    [Range(1, int.MaxValue)]
    public int EnrolmentId { get; set; }
}

public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int? Position { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public decimal? DistanceKm { get; set; }
    public DateTime RecordedAt { get; set; }
}
