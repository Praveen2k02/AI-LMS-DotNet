using AI_LMS.Domain.Entities;
using AI_LMS.Infrastructure.Services;
using Xunit;

namespace AI_LMS.Tests;

public sealed class ProgressServiceTests
{
    [Fact]
    public async Task Computes_completed_course_and_certificate_eligibility_from_database_records()
    {
        await using var db = TestDbFactory.Create();
        var role = new Role { Name = "Student" };
        var user = new User { Name = "Learner", Email = "student@example.com", PasswordHash = "hash", Role = role };
        user.Student = new Student { User = user, Department = "CS" };
        var course = new Course { Title = "Course", Description = "A course description", Category = "Web", Level = "Beginner" };
        db.Users.Add(user);
        db.Courses.Add(course);
        await db.SaveChangesAsync();
        var lesson = new Lesson { CourseId = course.Id, Title = "Lesson one", Order = 1 };
        var assignment = new Assignment { CourseId = course.Id, Title = "Project", Description = "Submit a project", MaximumMarks = 10, Status = AI_LMS.Domain.Enums.AssignmentStatus.Published };
        var quiz = new Quiz { CourseId = course.Id, Title = "Quiz", PassingMarks = 1, IsPublished = true };
        db.Lessons.Add(lesson);
        db.Assignments.Add(assignment);
        db.Quizzes.Add(quiz);
        db.Enrollments.Add(new Enrollment { CourseId = course.Id, StudentId = user.Student.Id, IsActive = true });
        await db.SaveChangesAsync();
        db.LessonProgress.Add(new LessonProgress { LessonId = lesson.Id, StudentId = user.Student.Id, IsCompleted = true, CompletedAtUtc = DateTime.UtcNow });
        db.Submissions.Add(new Submission { AssignmentId = assignment.Id, StudentId = user.Student.Id, Text = "Submitted", Status = AI_LMS.Domain.Enums.SubmissionStatus.Submitted });
        db.QuizAttempts.Add(new QuizAttempt { QuizId = quiz.Id, StudentId = user.Student.Id, Score = 1, TotalMarks = 1 });
        db.Marks.Add(new Mark { CourseId = course.Id, StudentId = user.Student.Id, Title = "Quiz", Source = "Quiz", Score = 1, MaximumScore = 1 });
        await db.SaveChangesAsync();

        var progress = (await new ProgressService(db).GetAsync(user.Student.Id)).Single();
        Assert.Equal(100m, progress.CompletionPercent);
        Assert.Equal(0, progress.RemainingLessons);
        Assert.Equal(100m, progress.QuizAveragePercent);
        Assert.True(progress.CertificateEligible);
    }
}
