
using AI_LMS.Domain.Common;

namespace AI_LMS.Domain.Entities;

public sealed class Quiz : AuditableEntity
{
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public required string Title { get; set; }
    public string? Description { get; set; }

    public int TimeLimitMinutes { get; set; } = 20;
    public decimal PassingMarks { get; set; }

    public bool IsPublished { get; set; }
    public bool IsAiGenerated { get; set; }

    public ICollection<Question> Questions { get; set; }
        = new List<Question>();

    public ICollection<QuizAttempt> Attempts { get; set; }
        = new List<QuizAttempt>();

    // Connects the quiz to students selected by the trainer.
    public ICollection<QuizAssignment> Assignments { get; set; }
        = new List<QuizAssignment>();
}
