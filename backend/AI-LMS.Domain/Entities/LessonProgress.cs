using AI_LMS.Domain.Common;

namespace AI_LMS.Domain.Entities;

public sealed class LessonProgress : AuditableEntity
{
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
