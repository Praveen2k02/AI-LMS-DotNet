using AI_LMS.Domain.Common;
using AI_LMS.Domain.Enums;

namespace AI_LMS.Domain.Entities;

public sealed class Course : AuditableEntity
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public required string Category { get; set; }
    public int DurationWeeks { get; set; } = 4;
    public required string Level { get; set; } = "Beginner";
    public int? TrainerId { get; set; }
    public Trainer? Trainer { get; set; }
    public CourseStatus Status { get; set; } = CourseStatus.Active;
    public decimal Fee { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}
