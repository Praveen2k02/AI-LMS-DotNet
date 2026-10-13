using AI_LMS.Application.DTOs;

namespace AI_LMS.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<UserDto?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);
}
