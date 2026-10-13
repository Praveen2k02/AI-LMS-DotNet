using AI_LMS.Application.DTOs;

namespace AI_LMS.Application.Interfaces;

public interface IProgressService
{
    Task<IReadOnlyList<StudentProgressDto>> GetAsync(int studentId, CancellationToken cancellationToken = default);
}
