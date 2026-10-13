using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace AI_LMS.API.Extensions;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected int CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id
        : throw new UnauthorizedAccessException("User id claim is missing.");
}
