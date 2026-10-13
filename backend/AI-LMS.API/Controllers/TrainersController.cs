using AI_LMS.API.Extensions;
using AI_LMS.Application.DTOs;
using AI_LMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI_LMS.API.Controllers;

[Route("api/trainers")]
[Authorize(Roles = "Admin,Trainer")]
public sealed class TrainersController(IUserService users) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List([FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(await users.ListAsync("Trainer", search, cancellationToken));
}
