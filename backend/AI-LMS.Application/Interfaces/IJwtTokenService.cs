using AI_LMS.Domain.Entities;

namespace AI_LMS.Application.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(User user, string roleName);
}
