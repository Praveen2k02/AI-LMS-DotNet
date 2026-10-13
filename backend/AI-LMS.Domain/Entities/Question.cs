using AI_LMS.Domain.Common;

namespace AI_LMS.Domain.Entities;

public sealed class Question : AuditableEntity
{
    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;
    public required string QuestionText { get; set; }
    public decimal Marks { get; set; } = 1;
    public string? Explanation { get; set; }
    public int Order { get; set; }
    public ICollection<QuizOption> Options { get; set; } = new List<QuizOption>();
}
