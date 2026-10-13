using AI_LMS.Domain.Common;

namespace AI_LMS.Domain.Entities;

public sealed class QuizOption : BaseEntity
{
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    public required string Text { get; set; }
    public string Label { get; set; } = "A";
    public bool IsCorrect { get; set; }
}
