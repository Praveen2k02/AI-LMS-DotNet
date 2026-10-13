using AI_LMS.API.Extensions;
using AI_LMS.Application.DTOs;
using AI_LMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI_LMS.API.Controllers;

[Route("api/attendance")]
[Authorize(Roles = "Admin,Trainer")]
public sealed class AttendanceController(IAttendanceService attendance) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AttendanceDto>>> List([FromQuery] int courseId, [FromQuery] DateOnly? date, CancellationToken cancellationToken) =>
        Ok(await attendance.ListAsync(courseId, date, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<AttendanceDto>> Mark(AttendanceRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await attendance.MarkAsync(request, CurrentUserId, cancellationToken));
}
