using AI_LMS.Domain.Common;
using AI_LMS.Domain.Enums;

namespace AI_LMS.Domain.Entities;

public sealed class Submission : AuditableEntity
{
    public int AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public string? Text { get; set; }
    public string? FileUrl { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;
    public decimal? Marks { get; set; }
    public string? Feedback { get; set; }
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? GradedAtUtc { get; set; }
}
