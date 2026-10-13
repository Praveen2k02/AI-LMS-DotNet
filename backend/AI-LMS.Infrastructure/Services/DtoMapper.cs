using AI_LMS.Application.DTOs;
using AI_LMS.Domain.Entities;

namespace AI_LMS.Infrastructure.Services;

internal static class DtoMapper
{
    public static UserDto User(User user) => new(
        user.Id,
        user.Name,
        user.Email,
        user.Role?.Name ?? "Student",
        user.Phone,
        user.Trainer?.Expertise,
        user.Student?.Department,
        user.Student?.Id,
        user.Trainer?.Id);

    public static CourseDto Course(Course course, int studentCount, int lessonCount, bool enrolled, decimal? progress) => new(
        course.Id,
        course.Title,
        course.Description,
        course.ThumbnailUrl,
        course.Category,
        course.DurationWeeks,
        course.Level,
        course.Trainer?.UserId,
        course.Trainer?.User?.Name,
        course.Status.ToString(),
        course.Fee,
        studentCount,
        lessonCount,
        enrolled,
        progress);

    public static LessonDto Lesson(Lesson lesson, bool completed = false) => new(
        lesson.Id, lesson.CourseId, lesson.Title, lesson.Description, lesson.Content,
        lesson.VideoUrl, lesson.DocumentUrl, lesson.Order, lesson.DurationMinutes, completed);
}
