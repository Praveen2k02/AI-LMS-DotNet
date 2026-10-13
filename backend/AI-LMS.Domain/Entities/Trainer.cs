using AI_LMS.Domain.Common;

namespace AI_LMS.Domain.Entities;

public sealed class Trainer : AuditableEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string? Expertise { get; set; }
    public string? Bio { get; set; }
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
