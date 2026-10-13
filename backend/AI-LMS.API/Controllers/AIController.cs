using AI_LMS.API.Extensions;
using AI_LMS.Application.DTOs;
using AI_LMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI_LMS.API.Controllers;

[Route("api/ai")]
[Authorize]
public sealed class AIController(IAIService ai) : ApiControllerBase
{
    // Admin, Trainer and Student can generate quizzes
    [Authorize(Roles = "Admin,Trainer,Student")]
    [HttpPost("generate-quiz")]
    public async Task<ActionResult<GenerateQuizResponse>> GenerateQuiz(
        GenerateQuizRequest request,
        CancellationToken cancellationToken) =>
        Ok(await ai.GenerateQuizAsync(request, CurrentUserId, cancellationToken));

    // Only Admin and Trainer can publish quizzes
    [Authorize(Roles = "Admin,Trainer")]
    [HttpPost("quizzes/{documentId}/publish")]
    public async Task<ActionResult<QuizDto>> PublishQuiz(
        string documentId,
        QuizRequest request,
        CancellationToken cancellationToken) =>
        StatusCode(
            StatusCodes.Status201Created,
            await ai.PublishQuizAsync(
                documentId,
                CurrentUserId,
                request,
                cancellationToken));

    // Admin, Trainer and Student can generate summaries
    [Authorize(Roles = "Admin,Trainer,Student")]
    [HttpPost("generate-summary")]
    public async Task<ActionResult<SummaryResponse>> GenerateSummary(
        TextRequest request,
        CancellationToken cancellationToken) =>
        Ok(await ai.GenerateSummaryAsync(
            request,
            CurrentUserId,
            cancellationToken));

    // Any authenticated user
    [HttpPost("explain")]
    public async Task<ActionResult<ExplainResponse>> Explain(
        ExplainRequest request,
        CancellationToken cancellationToken) =>
        Ok(await ai.ExplainAsync(
            request,
            CurrentUserId,
            cancellationToken));

    // Any authenticated user
    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponse>> Chat(
        ChatRequest request,
        CancellationToken cancellationToken) =>
        Ok(await ai.ChatAsync(
            request,
            CurrentUserId,
            cancellationToken));

    [HttpGet("conversations")]
    public async Task<ActionResult<IReadOnlyList<ConversationDto>>> Conversations(
        CancellationToken cancellationToken) =>
        Ok(await ai.ListConversationsAsync(
            CurrentUserId,
            cancellationToken));

    [HttpGet("conversations/{id}")]
    public async Task<ActionResult<ConversationDto>> Conversation(
        string id,
        CancellationToken cancellationToken)
    {
        var conversation = await ai.GetConversationAsync(
            id,
            CurrentUserId,
            cancellationToken);

        return conversation is null ? NotFound() : Ok(conversation);
    }

    [HttpDelete("conversations/{id}")]
    public async Task<IActionResult> DeleteConversation(
        string id,
        CancellationToken cancellationToken) =>
        await ai.DeleteConversationAsync(
            id,
            CurrentUserId,
            cancellationToken)
            ? NoContent()
            : NotFound();
}