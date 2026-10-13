using System.Security.Cryptography;
using AI_LMS.Application.DTOs;
using AI_LMS.Application.Exceptions;
using AI_LMS.Application.Interfaces;
using AI_LMS.Domain.Entities;
using AI_LMS.Domain.Services;
using AI_LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AI_LMS.Infrastructure.Services;

public sealed class ProgressService(ApplicationDbContext db) : IProgressService
{
    public async Task<IReadOnlyList<StudentProgressDto>> GetAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var student = await db.Students.AsNoTracking().SingleOrDefaultAsync(s => s.Id == studentId, cancellationToken)
            ?? throw new LmsException(404, "Student profile not found.");
        var enrollments = await db.Enrollments.AsNoTracking().Where(e => e.StudentId == studentId && e.IsActive)
            .Include(e => e.Course).ThenInclude(c => c.Trainer).ThenInclude(t => t!.User)
            .OrderBy(e => e.Course.Title).ToListAsync(cancellationToken);
        var results = new List<StudentProgressDto>();
        foreach (var enrollment in enrollments)
        {
            var course = enrollment.Course;
            var totalLessons = await db.Lessons.CountAsync(l => l.CourseId == course.Id, cancellationToken);
            var completedLessons = await db.LessonProgress.CountAsync(p => p.StudentId == studentId && p.IsCompleted && p.Lesson.CourseId == course.Id, cancellationToken);
            var totalAssignments = await db.Assignments.CountAsync(a => a.CourseId == course.Id && a.Status == Domain.Enums.AssignmentStatus.Published, cancellationToken);
            var submissions = await db.Submissions.AsNoTracking().Include(s => s.Assignment)
                .Where(s => s.StudentId == studentId && s.Assignment.CourseId == course.Id).ToListAsync(cancellationToken);
            var totalQuizzes = await db.Quizzes.CountAsync(q => q.CourseId == course.Id && q.IsPublished, cancellationToken);
            var attempts = await db.QuizAttempts.AsNoTracking().Include(a => a.Quiz)
                .Where(a => a.StudentId == studentId && a.Quiz.CourseId == course.Id).ToListAsync(cancellationToken);
            var marks = await db.Marks.AsNoTracking().Where(m => m.StudentId == studentId && m.CourseId == course.Id).ToListAsync(cancellationToken);
            var attendance = await db.Attendance.AsNoTracking().Where(a => a.StudentId == studentId && a.CourseId == course.Id).ToListAsync(cancellationToken);
            var cert = await db.Certificates.AsNoTracking().FirstOrDefaultAsync(c => c.StudentId == studentId && c.CourseId == course.Id, cancellationToken);

            var averageQuiz = attempts.Count == 0 ? 0 : Math.Round(attempts.Average(a => a.TotalMarks == 0 ? 0 : a.Score / a.TotalMarks * 100), 1);
            var averageMarks = marks.Count == 0 ? 0 : Math.Round(marks.Average(m => m.MaximumScore == 0 ? 0 : m.Score / m.MaximumScore * 100), 1);
            var attendancePercent = attendance.Count == 0 ? 0 : Math.Round((decimal)attendance.Count(a => a.IsPresent) / attendance.Count * 100, 1);
            var completion = ProgressCalculator.CourseCompletion(completedLessons, totalLessons, submissions.Count, totalAssignments, attempts.Count, totalQuizzes);
            var allAssignmentsSubmitted = totalAssignments == 0 || submissions.Count(s => s.Assignment.Status == Domain.Enums.AssignmentStatus.Published) >= totalAssignments;
            var allLessonsCompleted = totalLessons > 0 && completedLessons >= totalLessons;
            var passedQuiz = attempts.Any(a => a.Score >= a.Quiz.PassingMarks);
            var eligible = allLessonsCompleted && allAssignmentsSubmitted && passedQuiz;

            results.Add(new StudentProgressDto(
                course.Id, course.Title, course.Trainer?.User?.Name,
                totalLessons, completedLessons, Math.Max(0, totalLessons - completedLessons),
                totalAssignments, Math.Min(submissions.Count, totalAssignments), totalQuizzes, attempts.Count,
                averageQuiz, averageMarks, attendancePercent, completion, eligible, cert?.VerificationCode));
        }
        return results;
    }
}

public sealed class CertificateService(ApplicationDbContext db, IProgressService progressService) : ICertificateService
{
    public async Task<IReadOnlyList<CertificateDto>> ListAsync(int? studentUserId, CancellationToken cancellationToken = default)
    {
        var query = db.Certificates.AsNoTracking().Include(c => c.Course).Include(c => c.Student).ThenInclude(s => s.User).AsQueryable();
        if (studentUserId.HasValue) query = query.Where(c => c.Student.UserId == studentUserId.Value);
        return await query.OrderByDescending(c => c.IssueDateUtc).Select(c => Map(c)).ToListAsync(cancellationToken);
    }

    public async Task<CertificateDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var cert = await db.Certificates.AsNoTracking().Include(c => c.Course).Include(c => c.Student).ThenInclude(s => s.User)
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        return cert is null ? null : Map(cert);
    }

    public async Task<CertificateDto> IssueAsync(int courseId, int studentUserId, bool staffOverride, CancellationToken cancellationToken = default)
    {
        var student = await db.Students.Include(s => s.User).SingleOrDefaultAsync(s => s.UserId == studentUserId, cancellationToken)
            ?? throw new LmsException(404, "Student profile not found.");
        var course = await db.Courses.SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken)
            ?? throw new LmsException(404, "Course not found.");
        if (!await db.Enrollments.AnyAsync(e => e.CourseId == courseId && e.StudentId == student.Id, cancellationToken))
            throw new LmsException(400, "The student is not enrolled in this course.");
        var existing = await db.Certificates.Include(c => c.Course).Include(c => c.Student).ThenInclude(s => s.User)
            .SingleOrDefaultAsync(c => c.CourseId == courseId && c.StudentId == student.Id, cancellationToken);
        if (existing is not null) return Map(existing);
        if (!staffOverride)
        {
            var progress = await progressService.GetAsync(student.Id, cancellationToken);
            var courseProgress = progress.SingleOrDefault(p => p.CourseId == courseId);
            if (courseProgress is null || !courseProgress.CertificateEligible)
                throw new LmsException(400, "Complete every lesson and assignment and pass a quiz before claiming this certificate.");
        }
        var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
        var certificate = new Certificate
        {
            StudentId = student.Id, CourseId = courseId,
            CertificateNumber = $"AILMS-{DateTime.UtcNow:yyyy}-{courseId:D3}-{student.Id:D4}",
            VerificationCode = code,
            FinalScore = await AverageScoreAsync(student.Id, courseId, cancellationToken),
            IssueDateUtc = DateTime.UtcNow,
            CompletionDateUtc = DateTime.UtcNow
        };
        db.Certificates.Add(certificate);
        await db.SaveChangesAsync(cancellationToken);
        certificate = await db.Certificates.Include(c => c.Course).Include(c => c.Student).ThenInclude(s => s.User)
            .SingleAsync(c => c.Id == certificate.Id, cancellationToken);
        return Map(certificate);
    }

    public async Task<CertificateDto?> VerifyAsync(string code, CancellationToken cancellationToken = default)
    {
        var cert = await db.Certificates.AsNoTracking().Include(c => c.Course).Include(c => c.Student).ThenInclude(s => s.User)
            .SingleOrDefaultAsync(c => c.VerificationCode == code, cancellationToken);
        return cert is null ? null : Map(cert);
    }

    private async Task<decimal> AverageScoreAsync(int studentId, int courseId, CancellationToken cancellationToken)
    {
        var rows = await db.Marks.AsNoTracking().Where(m => m.StudentId == studentId && m.CourseId == courseId).ToListAsync(cancellationToken);
        return rows.Count == 0 ? 0 : Math.Round(rows.Average(m => m.MaximumScore == 0 ? 0 : m.Score / m.MaximumScore * 100), 2);
    }

    private static CertificateDto Map(Certificate c) => new(c.Id, c.CourseId, c.Course.Title, c.Student.User.Name,
        c.CertificateNumber, c.VerificationCode, c.FinalScore, c.IssueDateUtc, c.CompletionDateUtc);
}

public sealed class AttendanceService(ApplicationDbContext db) : IAttendanceService
{
    public async Task<IReadOnlyList<AttendanceDto>> ListAsync(int courseId, DateOnly? date, CancellationToken cancellationToken = default)
    {
        var query = db.Attendance.AsNoTracking().Include(a => a.Student).ThenInclude(s => s.User).Where(a => a.CourseId == courseId);
        if (date.HasValue) query = query.Where(a => a.Date == date.Value);
        return await query.OrderByDescending(a => a.Date).ThenBy(a => a.Student.User.Name)
            .Select(a => new AttendanceDto(a.Id, a.CourseId, a.StudentId, a.Student.User.Name, a.Date, a.IsPresent))
            .Take(1000).ToListAsync(cancellationToken);
    }

    public async Task<AttendanceDto> MarkAsync(AttendanceRequest request, int markedByUserId, CancellationToken cancellationToken = default)
    {
        if (!await db.Enrollments.AnyAsync(e => e.CourseId == request.CourseId && e.StudentId == request.StudentId, cancellationToken))
            throw new LmsException(400, "The student is not enrolled in this course.");
        var row = await db.Attendance.SingleOrDefaultAsync(a => a.CourseId == request.CourseId && a.StudentId == request.StudentId && a.Date == request.Date, cancellationToken);
        if (row is null)
        {
            row = new Attendance { CourseId = request.CourseId, StudentId = request.StudentId, Date = request.Date, IsPresent = request.IsPresent, MarkedByUserId = markedByUserId };
            db.Attendance.Add(row);
        }
        else
        {
            row.IsPresent = request.IsPresent;
            row.MarkedByUserId = markedByUserId;
            row.UpdatedAtUtc = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(cancellationToken);
        var studentName = await db.Students.Where(s => s.Id == row.StudentId).Select(s => s.User.Name).SingleAsync(cancellationToken);
        return new AttendanceDto(row.Id, row.CourseId, row.StudentId, studentName, row.Date, row.IsPresent);
    }
}
