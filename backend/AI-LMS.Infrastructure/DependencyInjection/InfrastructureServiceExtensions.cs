using System.Text;
using AI_LMS.Application.Interfaces;
using AI_LMS.Infrastructure.Authentication;
using AI_LMS.Infrastructure.Data;
using AI_LMS.Infrastructure.MongoDB;
using AI_LMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace AI_LMS.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var sqlConnection = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("ConnectionStrings:SqlServer is required.");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(sqlConnection, sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

        services.AddSingleton<MongoDbContext>();
        services.AddHttpClient<AIService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<ILearningService, LearningService>();
        services.AddScoped<IProgressService, ProgressService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICertificateService, CertificateService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IAIService>(sp => sp.GetRequiredService<AIService>());

        var jwtSecret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is required.");
        if (Encoding.UTF8.GetByteCount(jwtSecret) < 32)
            throw new InvalidOperationException("Jwt:Secret must be at least 32 bytes.");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"] ?? "AI-LMS",
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"] ?? "AI-LMS-Client",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
            };
        });
        services.AddAuthorization();
        return services;
    }
}
