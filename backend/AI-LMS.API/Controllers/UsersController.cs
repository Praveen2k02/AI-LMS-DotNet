using AI_LMS.API.Extensions;
using AI_LMS.Application.DTOs;
using AI_LMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI_LMS.API.Controllers;

[Route("api/users")]
[Authorize(Roles = "Admin")]
public sealed class UsersController(IUserService users) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List([FromQuery] string? role, [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(await users.ListAsync(role, search, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(UserCreateRequest request, CancellationToken cancellationToken)
    {
        var user = await users.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, user);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(int id, UserUpdateRequest request, CancellationToken cancellationToken)
    {
        var user = await users.UpdateAsync(id, request, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (id == CurrentUserId) return BadRequest(new { success = false, message = "You cannot delete your own account.", errors = Array.Empty<string>() });
        return await users.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
    }
}
