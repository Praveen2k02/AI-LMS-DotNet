using AI_LMS.Application.DTOs;

namespace AI_LMS.Application.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> ListAsync(string? role, string? search, CancellationToken cancellationToken = default);
    Task<UserDto> CreateAsync(UserCreateRequest request, CancellationToken cancellationToken = default);
    Task<UserDto?> UpdateAsync(int id, UserUpdateRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
