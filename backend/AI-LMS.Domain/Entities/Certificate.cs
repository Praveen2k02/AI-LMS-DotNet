using AI_LMS.Domain.Common;

namespace AI_LMS.Domain.Entities;

public sealed class Certificate : AuditableEntity
{
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public required string CertificateNumber { get; set; }
    public required string VerificationCode { get; set; }
    public decimal FinalScore { get; set; }
    public DateTime IssueDateUtc { get; set; } = DateTime.UtcNow;
    public DateTime CompletionDateUtc { get; set; } = DateTime.UtcNow;
}
