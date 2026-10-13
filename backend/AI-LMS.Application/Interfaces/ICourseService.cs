
using AI_LMS.Application.DTOs;

namespace AI_LMS.Application.Interfaces;

public interface ICourseService
{
    Task<IReadOnlyList<CourseDto>> ListAsync(
        int? userId,
        string? search,
        string? category,
        string? level,
        CancellationToken cancellationToken = default);

    Task<CourseDetailDto?> GetAsync(
        int id,
        int? userId,
        CancellationToken cancellationToken = default);

    Task<CourseDto> CreateAsync(
        CourseRequest request,
        int actorUserId,
        CancellationToken cancellationToken = default);

    Task<CourseDto?> UpdateAsync(
        int id,
        CourseRequest request,
        int actorUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int id,
        int actorUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task EnrollAsync(
        int courseId,
        int userId,
        int? targetStudentId = null,
        CancellationToken cancellationToken = default);

    Task UnenrollAsync(
        int courseId,
        int userId,
        int? targetStudentId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnrolledStudentDto>> ListEnrolledStudentsAsync(
        int courseId,
        int actorUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default);
}
