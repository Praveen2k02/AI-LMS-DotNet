
using AI_LMS.Application.DTOs;
using AI_LMS.Application.Exceptions;
using AI_LMS.Application.Interfaces;
using AI_LMS.Domain.Entities;
using AI_LMS.Domain.Services;
using AI_LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AI_LMS.Infrastructure.Services;

public sealed class CourseService(ApplicationDbContext db) : ICourseService
{
    public async Task<IReadOnlyList<CourseDto>> ListAsync(
        int? userId,
        string? search,
        string? category,
        string? level,
        CancellationToken cancellationToken = default)
    {
        var query = db.Courses
            .AsNoTracking()
            .Include(c => c.Trainer)
            .ThenInclude(t => t!.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(c =>
                c.Title.Contains(term) ||
                c.Description.Contains(term) ||
                c.Category.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(c => c.Category == category);

        if (!string.IsNullOrWhiteSpace(level))
            query = query.Where(c => c.Level == level);

        var courses = await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        var studentId = userId.HasValue
            ? await StudentIdForUserAsync(
                userId.Value, cancellationToken)
            : null;

        var result = new List<CourseDto>(courses.Count);

        foreach (var course in courses)
        {
            result.Add(await MapCourseAsync(
                course, studentId, cancellationToken));
        }

        return result;
    }

    public async Task<CourseDetailDto?> GetAsync(
        int id,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var course = await db.Courses
            .AsNoTracking()
            .AsSplitQuery()
            .Include(c => c.Trainer)
                .ThenInclude(t => t!.User)
            .Include(c => c.Lessons)
            .Include(c => c.Assignments)
                .ThenInclude(a => a.Submissions)
            .Include(c => c.Quizzes)
                .ThenInclude(q => q.Questions)
                .ThenInclude(q => q.Options)
            .Include(c => c.Enrollments)
                .ThenInclude(e => e.Student)
                .ThenInclude(s => s.User)
                .ThenInclude(u => u.Role)
            .SingleOrDefaultAsync(
                c => c.Id == id, cancellationToken);

        if (course is null)
            return null;

        var studentId = userId.HasValue
            ? await StudentIdForUserAsync(
                userId.Value, cancellationToken)
            : null;

        var dto = await MapCourseAsync(
            course, studentId, cancellationToken);

        // Completion records belong to the current student.
        // Restrict lesson IDs to this course.
        var courseLessonIds = course.Lessons
            .Select(l => l.Id)
            .ToList();

        var completed = studentId.HasValue && courseLessonIds.Count > 0
            ? await db.LessonProgress
                .AsNoTracking()
                .Where(p =>
                    p.StudentId == studentId.Value &&
                    p.IsCompleted &&
                    courseLessonIds.Contains(p.LessonId))
                .Select(p => p.LessonId)
                .ToListAsync(cancellationToken)
            : new List<int>();

        var isStaff = userId.HasValue &&
            await db.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .AnyAsync(
                    u => u.Id == userId.Value &&
                    (u.Role.Name == "Admin" ||
                     u.Role.Name == "Trainer"),
                    cancellationToken);

        var lessonDtos = course.Lessons
            .OrderBy(l => l.Order)
            .Select(l => DtoMapper.Lesson(
                l, completed.Contains(l.Id)))
            .ToList();

        var assignmentDtos = course.Assignments
            .OrderBy(a => a.DueAtUtc)
            .Select(a =>
            {
                var submission = studentId.HasValue
                    ? a.Submissions.FirstOrDefault(
                        s => s.StudentId == studentId.Value)
                    : null;

                return new AssignmentDto(
                    a.Id,
                    a.CourseId,
                    a.Title,
                    a.Description,
                    a.DueAtUtc,
                    a.MaximumMarks,
                    a.Status.ToString(),
                    submission is not null,
                    submission?.Marks,
                    submission?.Feedback,
                    a.AttachmentUrl);
            })
            .ToList();

        var quizDtos = course.Quizzes
            .OrderBy(q => q.CreatedAtUtc)
            .Select(q => LearningService.MapQuiz(q, isStaff))
            .ToList();

        var students = course.Enrollments
            .Where(e => e.IsActive && e.Student.User is not null)
            .Select(e => DtoMapper.User(e.Student.User))
            .ToList();

        return new CourseDetailDto(
            dto,
            lessonDtos,
            assignmentDtos,
            quizDtos,
            students);
    }

    public async Task<CourseDto> CreateAsync(
        CourseRequest request,
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        var actor = await db.Users
            .Include(u => u.Role)
            .Include(u => u.Trainer)
            .SingleOrDefaultAsync(
                u => u.Id == actorUserId, cancellationToken)
            ?? throw new LmsException(
                401, "User session is no longer valid.");

        int? trainerId = actor.Role.Name == "Trainer"
            ? actor.Trainer?.Id
            : null;

        if (actor.Role.Name == "Trainer" && trainerId is null)
            throw new LmsException(
                400, "Trainer profile is missing.");

        if (actor.Role.Name == "Admin" &&
            request.TrainerId.HasValue)
        {
            trainerId = await db.Trainers
                .Where(t => t.UserId == request.TrainerId.Value)
                .Select(t => (int?)t.Id)
                .SingleOrDefaultAsync(cancellationToken);

            if (!trainerId.HasValue)
                throw new LmsException(
                    400, "The selected trainer does not exist.");
        }

        var course = new Course
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category.Trim(),
            Level = request.Level,
            DurationWeeks = request.DurationWeeks,
            Fee = request.Fee,
            ThumbnailUrl = request.ThumbnailUrl,
            TrainerId = trainerId
        };

        db.Courses.Add(course);
        await db.SaveChangesAsync(cancellationToken);

        // Add a starter outline once when a course is created.
        await EnsureStarterLessonsAsync(
            course.Id, course.Title, course.Category, cancellationToken);

        course = await db.Courses
            .Include(c => c.Trainer)
            .ThenInclude(t => t!.User)
            .SingleAsync(c => c.Id == course.Id, cancellationToken);

        return await MapCourseAsync(
            course, null, cancellationToken);
    }

    public async Task<CourseDto?> UpdateAsync(
        int id,
        CourseRequest request,
        int actorUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var course = await db.Courses
            .Include(c => c.Trainer)
            .ThenInclude(t => t!.User)
            .SingleOrDefaultAsync(
                c => c.Id == id, cancellationToken);

        if (course is null)
            return null;

        if (!isAdmin)
        {
            var own = await db.Trainers.AnyAsync(
                t => t.UserId == actorUserId &&
                     t.Id == course.TrainerId,
                cancellationToken);

            if (!own)
                throw new LmsException(
                    403,
                    "Trainers may only update their own courses.");
        }

        int? selectedTrainerId = null;

        if (isAdmin && request.TrainerId.HasValue)
        {
            selectedTrainerId = await db.Trainers
                .Where(t => t.UserId == request.TrainerId.Value)
                .Select(t => (int?)t.Id)
                .SingleOrDefaultAsync(cancellationToken);

            if (!selectedTrainerId.HasValue)
                throw new LmsException(
                    400, "The selected trainer does not exist.");
        }

        course.Title = request.Title.Trim();
        course.Description = request.Description.Trim();
        course.Category = request.Category.Trim();
        course.Level = request.Level;
        course.DurationWeeks = request.DurationWeeks;
        course.Fee = request.Fee;
        course.ThumbnailUrl = request.ThumbnailUrl;

        if (isAdmin)
            course.TrainerId = selectedTrainerId;

        course.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return await MapCourseAsync(
            course, null, cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        int id,
        int actorUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var course = await db.Courses
            .SingleOrDefaultAsync(
                c => c.Id == id, cancellationToken);

        if (course is null)
            return false;

        if (!isAdmin &&
            !await db.Trainers.AnyAsync(
                t => t.UserId == actorUserId &&
                     t.Id == course.TrainerId,
                cancellationToken))
        {
            throw new LmsException(
                403,
                "Trainers may only delete their own courses.");
        }

        db.Courses.Remove(course);
        await db.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task EnrollAsync(
        int courseId,
        int userId,
        int? targetStudentId = null,
        CancellationToken cancellationToken = default)
    {
        var courseExists = await db.Courses.AnyAsync(
            c => c.Id == courseId &&
                 c.Status == Domain.Enums.CourseStatus.Active,
            cancellationToken);

        if (!courseExists)
            throw new LmsException(404, "Active course not found.");

        // Older courses may not have lessons yet. Populate a starter outline
        // only when the course currently has no lessons.
        var courseForOutline = await db.Courses
            .AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => new { c.Title, c.Category })
            .SingleAsync(cancellationToken);

        await EnsureStarterLessonsAsync(
            courseId, courseForOutline.Title, courseForOutline.Category,
            cancellationToken);

        var studentId = targetStudentId ??
            await StudentIdForUserAsync(userId, cancellationToken)
            ?? throw new LmsException(
                403, "Only a student can enroll in a course.");

        if (!await db.Students.AnyAsync(
            s => s.Id == studentId, cancellationToken))
        {
            throw new LmsException(
                404, "Student profile not found.");
        }

        if (await db.Enrollments.AnyAsync(
            e => e.CourseId == courseId &&
                 e.StudentId == studentId,
            cancellationToken))
        {
            throw new LmsException(
                409, "Student is already enrolled in this course.");
        }

        db.Enrollments.Add(new Enrollment
        {
            CourseId = courseId,
            StudentId = studentId,
            IsActive = true
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UnenrollAsync(
        int courseId,
        int userId,
        int? targetStudentId = null,
        CancellationToken cancellationToken = default)
    {
        var studentId = targetStudentId ??
            await StudentIdForUserAsync(userId, cancellationToken)
            ?? throw new LmsException(
                403, "Only a student can leave a course.");

        var enrollment = await db.Enrollments
            .SingleOrDefaultAsync(
                e => e.CourseId == courseId &&
                     e.StudentId == studentId,
                cancellationToken);

        if (enrollment is null)
            throw new LmsException(404, "Enrollment not found.");

        db.Enrollments.Remove(enrollment);
        await db.SaveChangesAsync(cancellationToken);
    }

    // Lists active enrollments for a course and includes
    // each student's lesson completion progress.
    // Admins can access any course; trainers can access their own courses.
    public async Task<IReadOnlyList<EnrolledStudentDto>>
        ListEnrolledStudentsAsync(
            int courseId,
            int actorUserId,
            bool isAdmin,
            CancellationToken cancellationToken = default)
    {
        var course = await db.Courses
            .AsNoTracking()
            .SingleOrDefaultAsync(
                c => c.Id == courseId, cancellationToken);

        if (course is null)
            throw new LmsException(404, "Course not found.");

        if (!isAdmin)
        {
            var ownsCourse = await db.Trainers.AnyAsync(
                t => t.UserId == actorUserId &&
                     t.Id == course.TrainerId,
                cancellationToken);

            if (!ownsCourse)
                throw new LmsException(
                    403,
                    "Trainers may only view students in their own courses.");
        }

        var totalLessons = await db.Lessons
            .CountAsync(
                l => l.CourseId == courseId,
                cancellationToken);

        var students = await db.Enrollments
            .AsNoTracking()
            .Where(e =>
                e.CourseId == courseId &&
                e.IsActive)
            .OrderBy(e => e.Student.User.Name)
            .Select(e => new
            {
                e.StudentId,
                UserId = e.Student.UserId,
                Name = e.Student.User.Name,
                Email = e.Student.User.Email,

                CompletedLessons = db.LessonProgress.Count(p =>
                    p.StudentId == e.StudentId &&
                    p.IsCompleted &&
                    p.Lesson.CourseId == courseId)
            })
            .ToListAsync(cancellationToken);

        return students
            .Select(s => new EnrolledStudentDto(
                s.StudentId,
                s.UserId,
                s.Name,
                s.Email,
                totalLessons,
                s.CompletedLessons,
                totalLessons == 0
                    ? 0m
                    : Math.Round(
                        s.CompletedLessons * 100m / totalLessons,
                        2)))
            .ToList();
    }

    private async Task EnsureStarterLessonsAsync(
        int courseId,
        string courseTitle,
        string category,
        CancellationToken cancellationToken)
    {
        // Never replace or duplicate lessons already created by a trainer.
        var hasLessons = await db.Lessons
            .AnyAsync(l => l.CourseId == courseId, cancellationToken);

        if (hasLessons)
            return;

        var outline = GetStarterOutline(courseTitle, category);

        var lessons = outline.Select((item, index) => new Lesson
        {
            CourseId = courseId,
            Title = item.Title,
            Description = item.Description,
            Content = item.Content,
            Order = index + 1,
            DurationMinutes = item.DurationMinutes
        });

        db.Lessons.AddRange(lessons);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<StarterLesson> GetStarterOutline(
        string courseTitle,
        string category)
    {
        var text = $"{courseTitle} {category}".ToLowerInvariant();

        if (text.Contains("react") || text.Contains("typescript"))
        {
            return new[]
            {
                new StarterLesson(
                    "Component Architecture",
                    "Learn how to structure reusable React components.",
                    "Understand components, props, composition, and how to organize a React application into reusable UI parts.",
                    45),
                new StarterLesson(
                    "React Hooks",
                    "Learn state and lifecycle behavior with React Hooks.",
                    "Study useState, useEffect, and custom hooks. Practice managing component state and side effects.",
                    45),
                new StarterLesson(
                    "Routing and Forms",
                    "Build navigation and forms in a React application.",
                    "Learn client-side routing, form inputs, validation, and handling user input.",
                    45),
                new StarterLesson(
                    "Testing and Accessibility",
                    "Learn basic component testing and accessible UI practices.",
                    "Explore component testing concepts, semantic HTML, keyboard navigation, and accessible interfaces.",
                    45)
            };
        }

        if (text.Contains("dotnet") || text.Contains(".net") ||
            text.Contains("asp.net") || text.Contains("c#") ||
            text.Contains("csharp"))
        {
            return new[]
            {
                new StarterLesson(
                    "C# Basics",
                    "Learn the fundamentals of the C# programming language.",
                    "Study variables, data types, operators, conditions, loops, methods, and basic exception handling in C#.",
                    45),
                new StarterLesson(
                    "Object-Oriented Programming",
                    "Understand the main object-oriented programming concepts.",
                    "Learn classes, objects, encapsulation, inheritance, polymorphism, abstraction, and interfaces in C#.",
                    45),
                new StarterLesson(
                    "ASP.NET Core Web API",
                    "Build HTTP APIs using ASP.NET Core.",
                    "Learn controllers or minimal APIs, routing, dependency injection, HTTP methods, status codes, and validation.",
                    60),
                new StarterLesson(
                    "Entity Framework Core",
                    "Connect a .NET application to a database using EF Core.",
                    "Learn DbContext, entities, LINQ queries, migrations, relationships, and CRUD operations.",
                    60)
            };
        }

        if (text.Contains("python"))
        {
            return new[]
            {
                new StarterLesson(
                    "Python Basics",
                    "Learn Python syntax and core language concepts.",
                    "Study variables, data types, operators, conditions, loops, and basic input and output.",
                    45),
                new StarterLesson(
                    "Functions",
                    "Organize reusable code with Python functions.",
                    "Learn parameters, return values, scope, default arguments, and importing modules.",
                    45),
                new StarterLesson(
                    "Object-Oriented Programming in Python",
                    "Use classes and objects to organize Python programs.",
                    "Learn classes, objects, constructors, inheritance, encapsulation, and method overriding.",
                    45),
                new StarterLesson(
                    "Data Analysis",
                    "Get an introduction to working with data in Python.",
                    "Learn the basics of lists and dictionaries, reading CSV data, and introductory analysis with common Python data tools.",
                    60)
            };
        }

        // Fallback outline for courses whose title does not match a known template.
        // The trainer can rename, edit, or remove these starter lessons.
        return new[]
        {
            new StarterLesson(
                $"{courseTitle}: Introduction",
                $"Introduction to {courseTitle}.",
                $"Learn the goals, key concepts, and practical applications of {courseTitle}.",
                30),
            new StarterLesson(
                $"{courseTitle}: Core Concepts",
                $"Study the core concepts of {courseTitle}.",
                $"Review the essential terminology, principles, and building blocks used in {courseTitle}.",
                45),
            new StarterLesson(
                $"{courseTitle}: Practical Skills",
                $"Apply the concepts of {courseTitle} in practice.",
                $"Work through guided examples and exercises related to {courseTitle}.",
                45),
            new StarterLesson(
                $"{courseTitle}: Review and Practice",
                $"Review the main topics and practice what you learned.",
                $"Consolidate the topics covered in {courseTitle} with a practical exercise and knowledge review.",
                30)
        };
    }

    private sealed record StarterLesson(
        string Title,
        string Description,
        string Content,
        int DurationMinutes);

    private async Task<int?> StudentIdForUserAsync(
        int userId,
        CancellationToken cancellationToken) =>
        await db.Students
            .Where(s => s.UserId == userId)
            .Select(s => (int?)s.Id)
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<CourseDto> MapCourseAsync(
        Course course,
        int? studentId,
        CancellationToken cancellationToken)
    {
        var enrolled = studentId.HasValue &&
            await db.Enrollments.AnyAsync(
                e => e.CourseId == course.Id &&
                     e.StudentId == studentId &&
                     e.IsActive,
                cancellationToken);

        var students = await db.Enrollments.CountAsync(
            e => e.CourseId == course.Id && e.IsActive,
            cancellationToken);

        var lessons = await db.Lessons.CountAsync(
            l => l.CourseId == course.Id,
            cancellationToken);

        decimal? progress = null;

        if (studentId.HasValue && enrolled)
        {
            var completed = await db.LessonProgress.CountAsync(
                p => p.StudentId == studentId &&
                     p.IsCompleted &&
                     p.Lesson.CourseId == course.Id,
                cancellationToken);

            var assignments = await db.Assignments.CountAsync(
                a => a.CourseId == course.Id,
                cancellationToken);

            var submitted = await db.Submissions.CountAsync(
                s => s.StudentId == studentId &&
                     s.Assignment.CourseId == course.Id,
                cancellationToken);

            var quizzes = await db.Quizzes.CountAsync(
                q => q.CourseId == course.Id && q.IsPublished,
                cancellationToken);

            var attempts = await db.QuizAttempts.CountAsync(
                a => a.StudentId == studentId &&
                     a.Quiz.CourseId == course.Id,
                cancellationToken);

            progress = ProgressCalculator.CourseCompletion(
                completed, lessons, submitted,
                assignments, attempts, quizzes);
        }

        return DtoMapper.Course(
            course, students, lessons, enrolled, progress);
    }
}
