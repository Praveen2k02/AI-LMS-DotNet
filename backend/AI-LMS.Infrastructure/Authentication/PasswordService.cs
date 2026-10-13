using AI_LMS.Application.Interfaces;
using AI_LMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace AI_LMS.Infrastructure.Authentication;

public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<User> _hasher = new();
    private static User ContextUser => new() { Name = "Password Context", Email = "context@ailms.local", PasswordHash = "" };

    public string Hash(string password) => _hasher.HashPassword(ContextUser, password);

    public bool Verify(string password, string passwordHash) =>
        _hasher.VerifyHashedPassword(ContextUser, passwordHash, password) != PasswordVerificationResult.Failed;
}
