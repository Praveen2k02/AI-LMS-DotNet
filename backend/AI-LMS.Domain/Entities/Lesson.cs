using AI_LMS.Domain.Common;

namespace AI_LMS.Domain.Entities;

public sealed class Lesson : AuditableEntity
{
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? Content { get; set; }
    public string? VideoUrl { get; set; }
    public string? DocumentUrl { get; set; }
    public int Order { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public ICollection<LessonProgress> Progress { get; set; } = new List<LessonProgress>();
}
