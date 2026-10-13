using AI_LMS.API.Extensions;
using AI_LMS.Application.DTOs;
using AI_LMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI_LMS.API.Controllers;

[Route("api/submissions")]
[Authorize(Roles = "Admin,Trainer")]
public sealed class SubmissionsController(ILearningService learning) : ApiControllerBase
{
    [HttpPost("{id:int}/grade")]
    public async Task<ActionResult<SubmissionDto>> Grade(int id, GradeRequest request, CancellationToken cancellationToken) =>
        Ok(await learning.GradeAsync(id, request, cancellationToken));
}
