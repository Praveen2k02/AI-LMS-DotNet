using AI_LMS.API.Extensions;
using AI_LMS.Application.DTOs;
using AI_LMS.Application.Exceptions;
using AI_LMS.Application.Interfaces;
using AI_LMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_LMS.API.Controllers;

[Route("api/progress")]
[Authorize]
public sealed class ProgressController(IProgressService progress, ApplicationDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StudentProgressDto>>> Get([FromQuery] int? studentUserId, CancellationToken cancellationToken)
    {
        var userId = User.IsInRole("Admin") || User.IsInRole("Trainer") ? studentUserId ?? CurrentUserId : CurrentUserId;
        var studentId = await db.Students.Where(s => s.UserId == userId).Select(s => (int?)s.Id).SingleOrDefaultAsync(cancellationToken)
            ?? throw new LmsException(404, "Student profile not found.");
        return Ok(await progress.GetAsync(studentId, cancellationToken));
    }
}
