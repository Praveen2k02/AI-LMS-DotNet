
using AI_LMS.API.Extensions;
using AI_LMS.Application.DTOs;
using AI_LMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI_LMS.API.Controllers;

[Route("api/courses")]
public sealed class CoursesController(ICourseService courses) : ApiControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CourseDto>>> List(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string? level,
        CancellationToken cancellationToken)
    {
        int? userId = User.Identity?.IsAuthenticated == true
            ? CurrentUserId
            : null;

        return Ok(await courses.ListAsync(
            userId, search, category, level, cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CourseDetailDto>> Get(
        int id,
        CancellationToken cancellationToken)
    {
        int? userId = User.Identity?.IsAuthenticated == true
            ? CurrentUserId
            : null;

        var course = await courses.GetAsync(
            id, userId, cancellationToken);

        return course is null ? NotFound() : Ok(course);
    }

    [Authorize(Roles = "Admin,Trainer")]
    [HttpGet("{id:int}/enrolled-students")]
    public async Task<ActionResult<IReadOnlyList<EnrolledStudentDto>>>
        ListEnrolledStudents(
            int id,
            CancellationToken cancellationToken)
    {
        var students = await courses.ListEnrolledStudentsAsync(
            id,
            CurrentUserId,
            User.IsInRole("Admin"),
            cancellationToken);

        return Ok(students);
    }

    [Authorize(Roles = "Admin,Trainer")]
    [HttpPost]
    public async Task<ActionResult<CourseDto>> Create(
        CourseRequest request,
        CancellationToken cancellationToken)
    {
        var course = await courses.CreateAsync(
            request, CurrentUserId, cancellationToken);

        return CreatedAtAction(
            nameof(Get), new { id = course.Id }, course);
    }

    [Authorize(Roles = "Admin,Trainer")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CourseDto>> Update(
        int id,
        CourseRequest request,
        CancellationToken cancellationToken)
    {
        var course = await courses.UpdateAsync(
            id, request, CurrentUserId,
            User.IsInRole("Admin"), cancellationToken);

        return course is null ? NotFound() : Ok(course);
    }

    [Authorize(Roles = "Admin,Trainer")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        var deleted = await courses.DeleteAsync(
            id, CurrentUserId,
            User.IsInRole("Admin"), cancellationToken);

        return deleted ? NoContent() : NotFound();
    }

    [Authorize]
    [HttpPost("{id:int}/enroll")]
    public async Task<IActionResult> Enroll(
        int id,
        EnrollmentRequest? request,
        CancellationToken cancellationToken)
    {
        var target = request?.StudentId;

        if (target.HasValue &&
            !User.IsInRole("Admin") &&
            !User.IsInRole("Trainer"))
        {
            return Forbid();
        }

        await courses.EnrollAsync(
            id, CurrentUserId, target, cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new
            {
                success = true,
                message = "Enrollment created."
            });
    }

    [Authorize]
    [HttpDelete("{id:int}/enroll")]
    public async Task<IActionResult> Unenroll(
        int id,
        [FromQuery] int? studentId,
        CancellationToken cancellationToken)
    {
        if (studentId.HasValue &&
            !User.IsInRole("Admin") &&
            !User.IsInRole("Trainer"))
        {
            return Forbid();
        }

        await courses.UnenrollAsync(
            id, CurrentUserId, studentId, cancellationToken);

        return NoContent();
    }
}
