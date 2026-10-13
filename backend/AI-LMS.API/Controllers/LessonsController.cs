using AI_LMS.API.Extensions;
using AI_LMS.Application.DTOs;
using AI_LMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI_LMS.API.Controllers;

[Route("api")]
public sealed class LessonsController(ILearningService learning) : ApiControllerBase
{
    [Authorize(Roles = "Admin,Trainer")]
    [HttpPost("courses/{courseId:int}/lessons")]
    public async Task<ActionResult<LessonDto>> Create(int courseId, LessonRequest request, CancellationToken cancellationToken)
    {
        var lesson = await learning.AddLessonAsync(courseId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, lesson);
    }

    [Authorize(Roles = "Admin,Trainer")]
    [HttpDelete("lessons/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await learning.DeleteLessonAsync(id, cancellationToken) ? NoContent() : NotFound();

    [Authorize(Roles = "Student")]
    [HttpPost("lessons/{id:int}/complete")]
    public async Task<ActionResult<LessonDto>> ToggleComplete(int id, CancellationToken cancellationToken) =>
        Ok(await learning.ToggleLessonAsync(id, CurrentUserId, cancellationToken));
}
