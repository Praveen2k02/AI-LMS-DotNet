
using System.ComponentModel.DataAnnotations;

namespace AI_LMS.Application.DTOs;

public sealed record UserDto(
    int Id,
    string Name,
    string Email,
    string Role,
    string? Phone,
    string? Expertise,
    string? Department,
    int? StudentId = null,
    int? TrainerId = null);

public sealed record AuthResponse(
    string Token,
    UserDto User,
    DateTime ExpiresAtUtc);

public sealed class LoginRequest
{
    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; init; } = "";

    [Required, MinLength(6), MaxLength(128)]
    public string Password { get; init; } = "";
}

public sealed class RegisterRequest
{
    [Required, MaxLength(100)]
    public string Name { get; init; } = "";

    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; init; } = "";

    [Required, MinLength(8), MaxLength(128)]
    public string Password { get; init; } = "";

    [MaxLength(30)]
    public string Role { get; init; } = "Student";

    [MaxLength(30)]
    public string? Phone { get; init; }

    [MaxLength(150)]
    public string? Expertise { get; init; }

    [MaxLength(100)]
    public string? Department { get; init; }
}

public sealed class CourseRequest
{
    [Required, StringLength(160, MinimumLength = 3)]
    public string Title { get; init; } = "";

    [Required, StringLength(5000, MinimumLength = 10)]
    public string Description { get; init; } = "";

    [Required, StringLength(80)]
    public string Category { get; init; } = "General";

    [Required, RegularExpression("^(Beginner|Intermediate|Advanced)$")]
    public string Level { get; init; } = "Beginner";

    [Range(1, 104)]
    public int DurationWeeks { get; init; } = 4;

    [Range(0, 10000000)]
    public decimal Fee { get; init; }

    public int? TrainerId { get; init; }

    public string? ThumbnailUrl { get; init; }
}

public sealed record CourseDto(
    int Id,
    string Title,
    string Description,
    string? ThumbnailUrl,
    string Category,
    int DurationWeeks,
    string Level,
    int? TrainerId,
    string? TrainerName,
    string Status,
    decimal Fee,
    int StudentCount,
    int LessonCount,
    bool IsEnrolled,
    decimal? ProgressPercent);

public sealed record LessonDto(
    int Id,
    int CourseId,
    string Title,
    string? Description,
    string? Content,
    string? VideoUrl,
    string? DocumentUrl,
    int Order,
    int DurationMinutes,
    bool IsCompleted);

public sealed record AssignmentDto(
    int Id,
    int CourseId,
    string Title,
    string Description,
    DateTime? DueAtUtc,
    decimal MaximumMarks,
    string Status,
    bool IsSubmitted,
    decimal? Marks,
    string? Feedback,
    string? AttachmentUrl);

public sealed record QuizDto(
    int Id,
    int CourseId,
    string Title,
    string? Description,
    int TimeLimitMinutes,
    decimal PassingMarks,
    bool IsPublished,
    bool IsAiGenerated,
    IReadOnlyList<QuestionDto> Questions);

public sealed record QuestionDto(
    int Id,
    string QuestionText,
    decimal Marks,
    string? Explanation,
    IReadOnlyList<QuizOptionDto> Options,
    int? CorrectOptionId);

public sealed record QuizOptionDto(
    int Id,
    string Label,
    string Text,
    bool? IsCorrect);

public sealed record CourseDetailDto(
    CourseDto Course,
    IReadOnlyList<LessonDto> Lessons,
    IReadOnlyList<AssignmentDto> Assignments,
    IReadOnlyList<QuizDto> Quizzes,
    IReadOnlyList<UserDto> Students);

public sealed class LessonRequest
{
    [Required, StringLength(160)]
    public string Title { get; init; } = "";

    [StringLength(2000)]
    public string? Description { get; init; }

    [StringLength(20000)]
    public string? Content { get; init; }

    [Url]
    public string? VideoUrl { get; init; }

    [Url]
    public string? DocumentUrl { get; init; }

    [Range(1, 1000)]
    public int DurationMinutes { get; init; } = 30;

    [Range(0, 10000)]
    public int? Order { get; init; }
}

public sealed class AssignmentRequest
{
    [Required, StringLength(160)]
    public string Title { get; init; } = "";

    [Required, StringLength(5000)]
    public string Description { get; init; } = "";

    public int? LessonId { get; init; }

    public DateTime? DueAtUtc { get; init; }

    [Range(1, 100000)]
    public decimal MaximumMarks { get; init; } = 100;

    [Url]
    public string? AttachmentUrl { get; init; }
}

public sealed class SubmissionRequest
{
    [StringLength(10000)]
    public string? Text { get; init; }

    [Url]
    public string? FileUrl { get; init; }
}

public sealed class GradeRequest
{
    [Range(0, 100000)]
    public decimal Marks { get; init; }

    [StringLength(2000)]
    public string? Feedback { get; init; }
}

public sealed class QuestionInput
{
    [Required, StringLength(1000)]
    public string Text { get; init; } = "";

    [Required, MinLength(2), MaxLength(6)]
    public List<string> Options { get; init; } = [];

    [Range(0, 5)]
    public int CorrectOptionIndex { get; init; }

    [Range(0.1, 1000)]
    public decimal Marks { get; init; } = 1;

    [StringLength(2000)]
    public string? Explanation { get; init; }
}

public sealed class QuizRequest
{
    [Required, StringLength(160)]
    public string Title { get; init; } = "";

    [StringLength(2000)]
    public string? Description { get; init; }

    [Range(1, 240)]
    public int TimeLimitMinutes { get; init; } = 20;

    [Range(0, 100000)]
    public decimal PassingMarks { get; init; }

    public bool Publish { get; init; } = true;

    public bool IsAiGenerated { get; init; }

    [Required, MinLength(1), MaxLength(100)]
    public List<QuestionInput> Questions { get; init; } = [];

    // Student IDs selected by the trainer for this quiz.
    [Required, MinLength(1)]
    public List<int> AssignedStudentIds { get; init; } = [];
}

public sealed class QuizAttemptRequest
{
    [Required, MinLength(1)]
    public List<AnswerInput> Answers { get; init; } = [];
}

public sealed class AnswerInput
{
    [Range(1, int.MaxValue)]
    public int QuestionId { get; init; }

    public int? SelectedOptionId { get; init; }
}

public sealed record QuizAttemptResult(
    int AttemptId,
    decimal Score,
    decimal TotalMarks,
    decimal Percentage,
    bool Passed,
    IReadOnlyList<AnswerReviewDto> Review);

public sealed record AnswerReviewDto(
    int QuestionId,
    string QuestionText,
    string? StudentAnswer,
    string CorrectAnswer,
    bool IsCorrect,
    decimal MarksAwarded,
    string? Explanation);

public sealed record StudentProgressDto(
    int CourseId,
    string CourseTitle,
    string? TrainerName,
    int TotalLessons,
    int CompletedLessons,
    int RemainingLessons,
    int TotalAssignments,
    int SubmittedAssignments,
    int TotalQuizzes,
    int QuizzesTaken,
    decimal QuizAveragePercent,
    decimal AverageMarksPercent,
    decimal AttendancePercent,
    decimal CompletionPercent,
    bool CertificateEligible,
    string? CertificateCode);

public sealed record MarkDto(
    int Id,
    int CourseId,
    string CourseTitle,
    string StudentName,
    string Source,
    string Title,
    decimal Score,
    decimal MaximumScore,
    decimal Percentage,
    string Grade,
    DateTime CreatedAtUtc);

public sealed record CertificateDto(
    int Id,
    int CourseId,
    string CourseTitle,
    string StudentName,
    string CertificateNumber,
    string VerificationCode,
    decimal FinalScore,
    DateTime IssueDateUtc,
    DateTime CompletionDateUtc);

public sealed class MarkRequest
{
    [Range(1, int.MaxValue)]
    public int CourseId { get; init; }

    [Range(1, int.MaxValue)]
    public int StudentId { get; init; }

    [Required, StringLength(160)]
    public string Title { get; init; } = "";

    [Required, StringLength(30)]
    public string Source { get; init; } = "Exam";

    [Range(0, 100000)]
    public decimal Score { get; init; }

    [Range(1, 100000)]
    public decimal MaximumScore { get; init; } = 100;
}

public sealed class EnrollmentRequest
{
    [Range(1, int.MaxValue)]
    public int? StudentId { get; init; }
}

public sealed class IssueCertificateRequest
{
    [Range(1, int.MaxValue)]
    public int CourseId { get; init; }

    [Range(1, int.MaxValue)]
    public int? StudentUserId { get; init; }
}

public sealed class AttendanceRequest
{
    [Range(1, int.MaxValue)]
    public int CourseId { get; init; }

    [Range(1, int.MaxValue)]
    public int StudentId { get; init; }

    public DateOnly Date { get; init; } =
        DateOnly.FromDateTime(DateTime.UtcNow);

    public bool IsPresent { get; init; } = true;
}

public sealed record AttendanceDto(
    int Id,
    int CourseId,
    int StudentId,
    string StudentName,
    DateOnly Date,
    bool IsPresent);

public sealed class GenerateQuizRequest
{
    [Required, StringLength(300)]
    public string Topic { get; init; } = "";

    [Range(1, 20)]
    public int Count { get; init; } = 5;

    [RegularExpression("^(Beginner|Intermediate|Advanced)$")]
    public string Difficulty { get; init; } = "Beginner";

    public int? CourseId { get; init; }

    public int? LessonId { get; init; }
}

public sealed record GeneratedQuestionDto(
    string Text,
    IReadOnlyList<string> Options,
    int CorrectOptionIndex,
    decimal Marks,
    string Explanation);

public sealed record GenerateQuizResponse(
    string DocumentId,
    bool UsedOpenAi,
    IReadOnlyList<GeneratedQuestionDto> Questions);

public sealed class TextRequest
{
    [Required, StringLength(12000, MinimumLength = 5)]
    public string Content { get; init; } = "";

    public int? CourseId { get; init; }

    public int? LessonId { get; init; }
}

public sealed record SummaryResponse(
    string DocumentId,
    bool UsedOpenAi,
    string Summary,
    IReadOnlyList<string> KeyPoints,
    IReadOnlyList<string> Concepts);

public sealed class ExplainRequest
{
    [Required, StringLength(2000)]
    public string Question { get; init; } = "";

    [Required, StringLength(1000)]
    public string StudentAnswer { get; init; } = "";

    [Required, StringLength(1000)]
    public string CorrectAnswer { get; init; } = "";
}

public sealed record ExplainResponse(
    string DocumentId,
    bool UsedOpenAi,
    string Explanation);

public sealed class ChatRequest
{
    [Required, StringLength(2000)]
    public string Message { get; init; } = "";

    public string? ConversationId { get; init; }
}

public sealed record ChatResponse(
    string ConversationId,
    bool UsedOpenAi,
    string Answer);

public sealed record ConversationDto(
    string Id,
    string Title,
    DateTime CreatedAtUtc,
    IReadOnlyList<ConversationMessageDto> Messages);

public sealed record ConversationMessageDto(
    string Role,
    string Content,
    DateTime CreatedAtUtc);

public sealed class UserCreateRequest
{
    [Required, StringLength(100)]
    public string Name { get; init; } = "";

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = "";

    [Required, MinLength(8)]
    public string Password { get; init; } = "";

    [Required, RegularExpression("^(Admin|Trainer|Student)$")]
    public string Role { get; init; } = "Student";

    public string? Phone { get; init; }

    public string? Expertise { get; init; }

    public string? Department { get; init; }
}


public sealed class UserUpdateRequest
{
    [Required, StringLength(100)]
    public string Name { get; init; } = "";

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = "";

    public string? Password { get; init; }

    public string? Phone { get; init; }

    public string? Expertise { get; init; }

    public string? Department { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed record EnrolledStudentDto(
    int StudentId,
    int UserId,
    string Name,
    string Email,
    int TotalLessons,
    int CompletedLessons,
    decimal ProgressPercent);
