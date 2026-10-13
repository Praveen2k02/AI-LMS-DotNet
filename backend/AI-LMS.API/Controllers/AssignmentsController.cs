using AI_LMS.API.Extensions;
using AI_LMS.Application.DTOs;
using AI_LMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI_LMS.API.Controllers;

[Route("api")]
public sealed class AssignmentsController(ILearningService learning) : ApiControllerBase
{
    [Authorize]
    [HttpGet("assignments")]
    public async Task<ActionResult<IReadOnlyList<AssignmentDto>>> List([FromQuery] int? courseId, CancellationToken cancellationToken)
    {
        var isStaff = User.IsInRole("Admin") || User.IsInRole("Trainer");
        return Ok(await learning.ListAssignmentsAsync(courseId, isStaff ? null : CurrentUserId, cancellationToken));
    }

    [Authorize(Roles = "Admin,Trainer")]
    [HttpPost("courses/{courseId:int}/assignments")]
    public async Task<ActionResult<AssignmentDto>> Create(int courseId, AssignmentRequest request, CancellationToken cancellationToken)
    {
        var assignment = await learning.CreateAssignmentAsync(courseId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, assignment);
    }

    [Authorize(Roles = "Admin,Trainer")]
    [HttpDelete("assignments/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await learning.DeleteAssignmentAsync(id, cancellationToken) ? NoContent() : NotFound();

    [Authorize(Roles = "Admin,Trainer")]
    [HttpGet("assignments/{id:int}/submissions")]
    public async Task<ActionResult<IReadOnlyList<SubmissionDto>>> Submissions(int id, CancellationToken cancellationToken) =>
        Ok(await learning.ListSubmissionsAsync(id, cancellationToken));

    [Authorize(Roles = "Student")]
    [HttpPost("assignments/{id:int}/submissions")]
    public async Task<ActionResult<SubmissionDto>> Submit(int id, SubmissionRequest request, CancellationToken cancellationToken)
    {
        var submission = await learning.SubmitAsync(id, CurrentUserId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, submission);
    }
}
