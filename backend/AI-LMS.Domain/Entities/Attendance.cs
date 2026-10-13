using AI_LMS.Domain.Common;

namespace AI_LMS.Domain.Entities;

public sealed class Attendance : AuditableEntity
{
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public DateOnly Date { get; set; }
    public bool IsPresent { get; set; }
    public int? MarkedByUserId { get; set; }
}
