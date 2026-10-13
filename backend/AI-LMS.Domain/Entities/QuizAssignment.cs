
using AI_LMS.Domain.Common;
using System;

namespace AI_LMS.Domain.Entities;

public sealed class QuizAssignment : AuditableEntity
{
    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
}
