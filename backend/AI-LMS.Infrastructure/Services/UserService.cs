using AI_LMS.Application.DTOs;
using AI_LMS.Application.Exceptions;
using AI_LMS.Application.Interfaces;
using AI_LMS.Domain.Entities;
using AI_LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AI_LMS.Infrastructure.Services;

public sealed class UserService(ApplicationDbContext db, IPasswordService passwords) : IUserService
{
    public async Task<IReadOnlyList<UserDto>> ListAsync(string? role, string? search, CancellationToken cancellationToken = default)
    {
        var query = db.Users.AsNoTracking().Include(u => u.Role).Include(u => u.Student).Include(u => u.Trainer).AsQueryable();
        if (!string.IsNullOrWhiteSpace(role)) query = query.Where(u => u.Role.Name == role);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u => u.Name.Contains(term) || u.Email.Contains(term));
        }
        return await query.OrderBy(u => u.Name).Take(500).Select(u => new UserDto(
            u.Id, u.Name, u.Email, u.Role.Name, u.Phone, u.Trainer == null ? null : u.Trainer.Expertise,
            u.Student == null ? null : u.Student.Department, u.Student == null ? null : u.Student.Id,
            u.Trainer == null ? null : u.Trainer.Id)).ToListAsync(cancellationToken);
    }

    public async Task<UserDto> CreateAsync(UserCreateRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
            throw new LmsException(409, "An account with this email already exists.");
        var role = await db.Roles.SingleOrDefaultAsync(r => r.Name == request.Role, cancellationToken)
            ?? throw new LmsException(400, "Role must be Admin, Trainer, or Student.");
        var user = new User
        {
            Name = request.Name.Trim(), Email = email, PasswordHash = passwords.Hash(request.Password),
            RoleId = role.Id, Role = role, Phone = request.Phone?.Trim(), IsActive = true
        };
        if (role.Name == "Trainer") user.Trainer = new Trainer { User = user, Expertise = request.Expertise, Bio = "" };
        if (role.Name == "Student") user.Student = new Student { User = user, Department = request.Department, JoinDateUtc = DateTime.UtcNow };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return new UserDto(user.Id, user.Name, user.Email, role.Name, user.Phone, user.Trainer?.Expertise, user.Student?.Department);
    }

    public async Task<UserDto?> UpdateAsync(int id, UserUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.Include(u => u.Role).Include(u => u.Student).Include(u => u.Trainer)
            .SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) return null;
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Id != id && u.Email == email, cancellationToken))
            throw new LmsException(409, "That email address is already in use.");
        user.Name = request.Name.Trim();
        user.Email = email;
        user.Phone = request.Phone?.Trim();
        user.IsActive = request.IsActive;
        if (!string.IsNullOrWhiteSpace(request.Password)) user.PasswordHash = passwords.Hash(request.Password);
        if (user.Trainer is not null)
        {
            user.Trainer.Expertise = request.Expertise?.Trim();
        }
        if (user.Student is not null) user.Student.Department = request.Department?.Trim();
        user.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return DtoMapper.User(user);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) return false;
        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
