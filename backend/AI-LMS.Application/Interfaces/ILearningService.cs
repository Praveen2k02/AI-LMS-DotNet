using AI_LMS.Application.DTOs;

namespace AI_LMS.Application.Interfaces;

public interface ILearningService
{
    Task<LessonDto> AddLessonAsync(int courseId, LessonRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteLessonAsync(int id, CancellationToken cancellationToken = default);
    Task<LessonDto> ToggleLessonAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task<AssignmentDto> CreateAssignmentAsync(int courseId, AssignmentRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAssignmentAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssignmentDto>> ListAssignmentsAsync(int? courseId, int? userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubmissionDto>> ListSubmissionsAsync(int assignmentId, CancellationToken cancellationToken = default);
    Task<SubmissionDto> SubmitAsync(int assignmentId, int userId, SubmissionRequest request, CancellationToken cancellationToken = default);
    Task<SubmissionDto> GradeAsync(int submissionId, GradeRequest request, CancellationToken cancellationToken = default);
    Task<QuizDto> CreateQuizAsync(int courseId, QuizRequest request, CancellationToken cancellationToken = default);
    Task<QuizDto?> GetQuizAsync(int quizId, int? userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteQuizAsync(int quizId, CancellationToken cancellationToken = default);
    Task<QuizAttemptResult> SubmitQuizAsync(int quizId, int userId, QuizAttemptRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MarkDto>> ListMarksAsync(int? userId, int? courseId, CancellationToken cancellationToken = default);
    Task<MarkDto> AddMarkAsync(MarkRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteMarkAsync(int id, CancellationToken cancellationToken = default);
}

public sealed record SubmissionDto(int Id, int AssignmentId, string AssignmentTitle, int StudentId, string StudentName, string? Text, string? FileUrl, string Status, decimal? Marks, decimal MaximumMarks, string? Feedback, DateTime SubmittedAtUtc);
