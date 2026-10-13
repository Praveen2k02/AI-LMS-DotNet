using AI_LMS.Domain.Common;

namespace AI_LMS.Domain.Entities;

public sealed class QuizAttempt : AuditableEntity
{
    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public decimal Score { get; set; }
    public decimal TotalMarks { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<StudentAnswer> Answers { get; set; } = new List<StudentAnswer>();
}
