using AI_LMS.Application.DTOs;

namespace AI_LMS.Application.Interfaces;

public interface IAIService
{
    Task<GenerateQuizResponse> GenerateQuizAsync(
        GenerateQuizRequest request,
        int userId,
        CancellationToken cancellationToken = default);

    Task<QuizDto> PublishQuizAsync(
        string documentId,
        int userId,
        QuizRequest editedQuiz,
        CancellationToken cancellationToken = default);

    Task<SummaryResponse> GenerateSummaryAsync(
        TextRequest request,
        int userId,
        CancellationToken cancellationToken = default);

    Task<ExplainResponse> ExplainAsync(
        ExplainRequest request,
        int userId,
        CancellationToken cancellationToken = default);

    Task<ChatResponse> ChatAsync(
        ChatRequest request,
        int userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationDto>> ListConversationsAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<ConversationDto?> GetConversationAsync(
        string id,
        int userId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteConversationAsync(
        string id,
        int userId,
        CancellationToken cancellationToken = default);
}