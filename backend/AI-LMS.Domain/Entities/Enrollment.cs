using AI_LMS.Domain.Common;

namespace AI_LMS.Domain.Entities;

public sealed class Enrollment : AuditableEntity
{
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public DateTime EnrolledAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
