using AI_LMS.Application.DTOs;
using AI_LMS.Domain.Entities;
using AI_LMS.Infrastructure.Authentication;
using AI_LMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AI_LMS.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task Register_hashes_password_and_returns_a_valid_jwt()
    {
        await using var db = TestDbFactory.Create();

        db.Roles.AddRange(
            new Role { Name = "Admin" },
            new Role { Name = "Trainer" },
            new Role { Name = "Student" }
        );

        await db.SaveChangesAsync();

        var password = new PasswordService();
        var jwt = new JwtTokenService(Configuration());
        var service = new AuthService(db, password, jwt);

        var response = await service.RegisterAsync(
            new RegisterRequest
            {
                Name = "A Learner",
                Email = "learner@example.com",
                Password = "safePass123",
                Role = "Student"
            });

        Assert.Equal("learner@example.com", response.User.Email);
        Assert.Equal("Student", response.User.Role);

        var user = await db.Users.SingleAsync();

        Assert.NotEqual(
            "safePass123",
            user.PasswordHash
        );

        Assert.True(
            password.Verify(
                "safePass123",
                user.PasswordHash
            )
        );

        Assert.False(
            password.Verify(
                "wrongPass123",
                user.PasswordHash
            )
        );

        Assert.False(
            string.IsNullOrWhiteSpace(response.Token)
        );
    }

    [Fact]
    public async Task Login_rejects_unknown_password()
    {
        await using var db = TestDbFactory.Create();

        db.Roles.Add(
            new Role
            {
                Name = "Student"
            }
        );

        await db.SaveChangesAsync();

        var password = new PasswordService();

        var service = new AuthService(
            db,
            password,
            new JwtTokenService(Configuration())
        );

        await service.RegisterAsync(
            new RegisterRequest
            {
                Name = "A Learner",
                Email = "learner@example.com",
                Password = "safePass123",
                Role = "Student"
            });

        await Assert.ThrowsAsync<AI_LMS.Application.Exceptions.LmsException>(
            () =>
                service.LoginAsync(
                    new LoginRequest
                    {
                        Email = "learner@example.com",
                        Password = "notThePassword"
                    })
        );
    }

    [Fact]
    public async Task Duplicate_email_is_rejected_case_insensitively()
    {
        await using var db = TestDbFactory.Create();

        db.Roles.Add(
            new Role
            {
                Name = "Student"
            }
        );

        await db.SaveChangesAsync();

        var service = new AuthService(
            db,
            new PasswordService(),
            new JwtTokenService(Configuration())
        );

        await service.RegisterAsync(
            new RegisterRequest
            {
                Name = "One",
                Email = "learner@example.com",
                Password = "safePass123",
                Role = "Student"
            });

        await Assert.ThrowsAsync<AI_LMS.Application.Exceptions.LmsException>(
            () =>
                service.RegisterAsync(
                    new RegisterRequest
                    {
                        Name = "Two",
                        Email = "LEARNER@example.com",
                        Password = "safePass456",
                        Role = "Student"
                    })
        );
    }

    private static IConfiguration Configuration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:Secret"] =
                        "unit-test-secret-key-must-be-at-least-32-bytes-2026",

                    ["Jwt:Issuer"] =
                        "AI-LMS",

                    ["Jwt:Audience"] =
                        "AI-LMS-Client"
                })
            .Build();
    }
}