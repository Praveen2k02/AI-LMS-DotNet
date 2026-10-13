using AI_LMS.Domain.Common;
using AI_LMS.Domain.Enums;

namespace AI_LMS.Domain.Entities;

public sealed class Assignment : AuditableEntity
{
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int? LessonId { get; set; }
    public Lesson? Lesson { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public decimal MaximumMarks { get; set; } = 100;
    public string? AttachmentUrl { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Published;
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
