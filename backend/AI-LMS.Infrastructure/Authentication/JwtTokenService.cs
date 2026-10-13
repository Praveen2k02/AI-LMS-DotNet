using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AI_LMS.Application.Interfaces;
using AI_LMS.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AI_LMS.Infrastructure.Authentication;

public sealed class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public (string Token, DateTime ExpiresAtUtc) CreateToken(User user, string roleName)
    {
        var secret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret must be configured.");
        if (Encoding.UTF8.GetByteCount(secret) < 32)
            throw new InvalidOperationException("Jwt:Secret must be at least 32 bytes.");

        var expires = DateTime.UtcNow.AddMinutes(configuration.GetValue("Jwt:ExpiresMinutes", 120));
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, roleName)
        };
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "AI-LMS",
            audience: configuration["Jwt:Audience"] ?? "AI-LMS-Client",
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: credentials);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
