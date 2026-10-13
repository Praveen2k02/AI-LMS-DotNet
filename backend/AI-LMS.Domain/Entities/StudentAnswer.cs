using AI_LMS.Domain.Common;

namespace AI_LMS.Domain.Entities;

public sealed class StudentAnswer : BaseEntity
{
    public int QuizAttemptId { get; set; }
    public QuizAttempt QuizAttempt { get; set; } = null!;
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    public int? SelectedOptionId { get; set; }
    public QuizOption? SelectedOption { get; set; }
    public bool IsCorrect { get; set; }
    public decimal MarksAwarded { get; set; }
}
