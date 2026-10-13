using AI_LMS.API.Extensions;
using AI_LMS.Application.DTOs;
using AI_LMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI_LMS.API.Controllers;

[Route("api")]
public sealed class QuizzesController(ILearningService learning) : ApiControllerBase
{
    [Authorize(Roles = "Admin,Trainer")]
    [HttpPost("courses/{courseId:int}/quizzes")]
    public async Task<ActionResult<QuizDto>> Create(int courseId, QuizRequest request, CancellationToken cancellationToken)
    {
        var quiz = await learning.CreateQuizAsync(courseId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, quiz);
    }

    [Authorize]
    [HttpGet("quizzes/{id:int}")]
    public async Task<ActionResult<QuizDto>> Get(int id, CancellationToken cancellationToken)
    {
        var quiz = await learning.GetQuizAsync(id, CurrentUserId, cancellationToken);
        return quiz is null ? NotFound() : Ok(quiz);
    }

    [Authorize(Roles = "Admin,Trainer")]
    [HttpDelete("quizzes/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await learning.DeleteQuizAsync(id, cancellationToken) ? NoContent() : NotFound();

    [Authorize(Roles = "Student")]
    [HttpPost("quizzes/{id:int}/attempt")]
    public async Task<ActionResult<QuizAttemptResult>> Attempt(int id, QuizAttemptRequest request, CancellationToken cancellationToken) =>
        Ok(await learning.SubmitQuizAsync(id, CurrentUserId, request, cancellationToken));
}
