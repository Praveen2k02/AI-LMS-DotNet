using AI_LMS.Domain.Common;

namespace AI_LMS.Domain.Entities;

public sealed class Mark : AuditableEntity
{
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public required string Source { get; set; }
    public required string Title { get; set; }
    public decimal Score { get; set; }
    public decimal MaximumScore { get; set; }
}
