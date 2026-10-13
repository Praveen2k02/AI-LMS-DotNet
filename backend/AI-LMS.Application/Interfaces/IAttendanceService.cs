using AI_LMS.Application.DTOs;

namespace AI_LMS.Application.Interfaces;

public interface IAttendanceService
{
    Task<IReadOnlyList<AttendanceDto>> ListAsync(int courseId, DateOnly? date, CancellationToken cancellationToken = default);
    Task<AttendanceDto> MarkAsync(AttendanceRequest request, int markedByUserId, CancellationToken cancellationToken = default);
}
