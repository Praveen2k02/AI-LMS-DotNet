using AI_LMS.API.Extensions;
using AI_LMS.Application.DTOs;
using AI_LMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI_LMS.API.Controllers;

[Route("api/marks")]
[Authorize]
public sealed class MarksController(ILearningService learning) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MarkDto>>> List([FromQuery] int? courseId, [FromQuery] int? studentUserId, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Trainer")) studentUserId = CurrentUserId;
        return Ok(await learning.ListMarksAsync(studentUserId, courseId, cancellationToken));
    }

    [Authorize(Roles = "Admin,Trainer")]
    [HttpPost]
    public async Task<ActionResult<MarkDto>> Add(MarkRequest request, CancellationToken cancellationToken)
    {
        var mark = await learning.AddMarkAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, mark);
    }

    [Authorize(Roles = "Admin,Trainer")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await learning.DeleteMarkAsync(id, cancellationToken) ? NoContent() : NotFound();
}
