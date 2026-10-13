using AI_LMS.Application.DTOs;
using AI_LMS.Application.Exceptions;
using AI_LMS.Application.Interfaces;
using AI_LMS.Domain.Entities;
using AI_LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AI_LMS.Infrastructure.Services;

public sealed class AuthService(
    ApplicationDbContext db,
    IPasswordService passwords,
    IJwtTokenService jwt) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
            throw new LmsException(409, "An account with this email already exists.");

        var roleName = string.Equals(request.Role, "Trainer", StringComparison.OrdinalIgnoreCase) ? "Trainer" : "Student";
        var role = await db.Roles.SingleAsync(r => r.Name == roleName, cancellationToken);
        var user = new User
        {
            Name = request.Name.Trim(), Email = email, PasswordHash = passwords.Hash(request.Password),
            RoleId = role.Id, Role = role, Phone = request.Phone?.Trim(), IsActive = true
        };
        if (roleName == "Trainer")
            user.Trainer = new Trainer { User = user, Expertise = request.Expertise?.Trim(), Bio = "" };
        else
            user.Student = new Student { User = user, Department = request.Department?.Trim(), JoinDateUtc = DateTime.UtcNow };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users
            .Include(u => u.Role).Include(u => u.Student).Include(u => u.Trainer)
            .SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null || !user.IsActive || !passwords.Verify(request.Password, user.PasswordHash))
            throw new LmsException(401, "Email or password is incorrect.");
        return CreateAuthResponse(user);
    }

    public async Task<UserDto?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking()
            .Include(u => u.Role).Include(u => u.Student).Include(u => u.Trainer)
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        return user is null ? null : DtoMapper.User(user);
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var (token, expiresAt) = jwt.CreateToken(user, user.Role.Name);
        return new AuthResponse(token, DtoMapper.User(user), expiresAt);
    }
}
