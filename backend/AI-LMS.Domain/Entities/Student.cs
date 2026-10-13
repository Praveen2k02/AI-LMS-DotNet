using AI_LMS.Domain.Common;

namespace AI_LMS.Domain.Entities;

public sealed class Student : AuditableEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string? Department { get; set; }
    public DateTime JoinDateUtc { get; set; } = DateTime.UtcNow;
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<LessonProgress> LessonProgress { get; set; } = new List<LessonProgress>();
}
