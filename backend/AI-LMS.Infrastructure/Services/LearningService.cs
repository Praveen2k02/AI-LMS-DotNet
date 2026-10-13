
using AI_LMS.Application.DTOs;
using AI_LMS.Application.Exceptions;
using AI_LMS.Application.Interfaces;
using AI_LMS.Domain.Entities;
using AI_LMS.Domain.Enums;
using AI_LMS.Domain.Services;
using AI_LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AI_LMS.Infrastructure.Services;

public sealed class LearningService(ApplicationDbContext db) : ILearningService
{
    public async Task<LessonDto> AddLessonAsync(
        int courseId, LessonRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Courses.AnyAsync(c => c.Id == courseId, cancellationToken))
            throw new LmsException(404, "Course not found.");

        var order = request.Order ??
            ((await db.Lessons.Where(l => l.CourseId == courseId)
                .Select(l => (int?)l.Order).MaxAsync(cancellationToken) ?? 0) + 1);

        if (await db.Lessons.AnyAsync(
            l => l.CourseId == courseId && l.Order == order, cancellationToken))
            throw new LmsException(409, "A lesson already uses that order number.");

        var lesson = new Lesson
        {
            CourseId = courseId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Content = request.Content?.Trim(),
            VideoUrl = request.VideoUrl,
            DocumentUrl = request.DocumentUrl,
            DurationMinutes = request.DurationMinutes,
            Order = order
        };

        db.Lessons.Add(lesson);
        await db.SaveChangesAsync(cancellationToken);
        return DtoMapper.Lesson(lesson);
    }

    public async Task<bool> DeleteLessonAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var lesson = await db.Lessons
            .SingleOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (lesson is null) return false;

        db.Lessons.Remove(lesson);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<LessonDto> ToggleLessonAsync(
        int id, int userId, CancellationToken cancellationToken = default)
    {
        var lesson = await db.Lessons
            .SingleOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new LmsException(404, "Lesson not found.");

        var student = await db.Students
            .SingleOrDefaultAsync(s => s.UserId == userId, cancellationToken)
            ?? throw new LmsException(403, "Only students can track lesson progress.");

        if (!await db.Enrollments.AnyAsync(
            e => e.CourseId == lesson.CourseId &&
                 e.StudentId == student.Id && e.IsActive,
            cancellationToken))
            throw new LmsException(403, "Enroll in the course before tracking lessons.");

        var row = await db.LessonProgress.SingleOrDefaultAsync(
            p => p.LessonId == id && p.StudentId == student.Id,
            cancellationToken);

        if (row is null)
        {
            row = new LessonProgress
            {
                LessonId = id,
                StudentId = student.Id,
                IsCompleted = true,
                CompletedAtUtc = DateTime.UtcNow
            };
            db.LessonProgress.Add(row);
        }
        else
        {
            row.IsCompleted = !row.IsCompleted;
            row.CompletedAtUtc = row.IsCompleted ? DateTime.UtcNow : null;
            row.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        return DtoMapper.Lesson(lesson, row.IsCompleted);
    }

    public async Task<AssignmentDto> CreateAssignmentAsync(
        int courseId, AssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Courses.AnyAsync(c => c.Id == courseId, cancellationToken))
            throw new LmsException(404, "Course not found.");

        if (request.DueAtUtc.HasValue &&
            request.DueAtUtc.Value <= DateTime.UtcNow)
            throw new LmsException(400, "Due date must be in the future.");

        if (request.LessonId.HasValue &&
            !await db.Lessons.AnyAsync(
                l => l.Id == request.LessonId && l.CourseId == courseId,
                cancellationToken))
            throw new LmsException(400, "The selected lesson is not part of this course.");

        var assignment = new Assignment
        {
            CourseId = courseId,
            LessonId = request.LessonId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            DueAtUtc = request.DueAtUtc,
            MaximumMarks = request.MaximumMarks,
            AttachmentUrl = request.AttachmentUrl,
            Status = AssignmentStatus.Published
        };

        db.Assignments.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);
        return MapAssignment(assignment, null);
    }

    public async Task<bool> DeleteAssignmentAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var assignment = await db.Assignments
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (assignment is null) return false;

        db.Assignments.Remove(assignment);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<AssignmentDto>> ListAssignmentsAsync(
        int? courseId, int? userId,
        CancellationToken cancellationToken = default)
    {
        var query = db.Assignments.AsNoTracking()
            .Include(a => a.Submissions).AsQueryable();

        if (courseId.HasValue)
            query = query.Where(a => a.CourseId == courseId.Value);

        int? studentId = null;

        if (userId.HasValue)
        {
            studentId = await db.Students
                .Where(s => s.UserId == userId.Value)
                .Select(s => (int?)s.Id)
                .SingleOrDefaultAsync(cancellationToken);

            if (studentId.HasValue)
                query = query.Where(a => a.Course.Enrollments.Any(
                    e => e.StudentId == studentId.Value && e.IsActive));
        }

        var rows = await query.OrderBy(a => a.DueAtUtc)
            .Take(300).ToListAsync(cancellationToken);

        return rows.Select(a => MapAssignment(
            a,
            studentId.HasValue
                ? a.Submissions.FirstOrDefault(s => s.StudentId == studentId.Value)
                : null)).ToList();
    }

    public async Task<IReadOnlyList<SubmissionDto>> ListSubmissionsAsync(
        int assignmentId, CancellationToken cancellationToken = default)
    {
        if (!await db.Assignments.AnyAsync(
            a => a.Id == assignmentId, cancellationToken))
            throw new LmsException(404, "Assignment not found.");

        var rows = await db.Submissions.AsNoTracking()
            .Include(s => s.Student).ThenInclude(s => s.User)
            .Include(s => s.Assignment)
            .Where(s => s.AssignmentId == assignmentId)
            .OrderByDescending(s => s.SubmittedAtUtc)
            .ToListAsync(cancellationToken);

        return rows.Select(MapSubmission).ToList();
    }

    public async Task<SubmissionDto> SubmitAsync(
        int assignmentId, int userId, SubmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var assignment = await db.Assignments.Include(a => a.Course)
            .SingleOrDefaultAsync(a => a.Id == assignmentId, cancellationToken)
            ?? throw new LmsException(404, "Assignment not found.");

        var student = await db.Students
            .SingleOrDefaultAsync(s => s.UserId == userId, cancellationToken)
            ?? throw new LmsException(403, "Only students can submit assignments.");

        if (!await db.Enrollments.AnyAsync(
            e => e.CourseId == assignment.CourseId &&
                 e.StudentId == student.Id && e.IsActive,
            cancellationToken))
            throw new LmsException(403, "Enroll in the course before submitting.");

        if (assignment.DueAtUtc.HasValue &&
            assignment.DueAtUtc.Value < DateTime.UtcNow)
            throw new LmsException(400, "The assignment deadline has passed.");

        if (string.IsNullOrWhiteSpace(request.Text) &&
            string.IsNullOrWhiteSpace(request.FileUrl))
            throw new LmsException(400, "Provide submission text or a file URL.");

        var row = await db.Submissions.SingleOrDefaultAsync(
            s => s.AssignmentId == assignmentId && s.StudentId == student.Id,
            cancellationToken);

        if (row is { Status: SubmissionStatus.Graded })
            throw new LmsException(
                409, "This submission has already been graded and cannot be changed.");

        if (row is null)
        {
            row = new Submission
            {
                AssignmentId = assignmentId,
                StudentId = student.Id
            };
            db.Submissions.Add(row);
        }

        row.Text = request.Text?.Trim();
        row.FileUrl = request.FileUrl;
        row.SubmittedAtUtc = DateTime.UtcNow;
        row.Status = SubmissionStatus.Submitted;
        row.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        row = await db.Submissions.Include(s => s.Student)
            .ThenInclude(s => s.User).Include(s => s.Assignment)
            .SingleAsync(s => s.Id == row.Id, cancellationToken);

        return MapSubmission(row);
    }

    public async Task<SubmissionDto> GradeAsync(
        int submissionId, GradeRequest request,
        CancellationToken cancellationToken = default)
    {
        var row = await db.Submissions.Include(s => s.Assignment)
            .Include(s => s.Student).ThenInclude(s => s.User)
            .SingleOrDefaultAsync(s => s.Id == submissionId, cancellationToken)
            ?? throw new LmsException(404, "Submission not found.");

        if (request.Marks > row.Assignment.MaximumMarks)
            throw new LmsException(400, "Marks cannot exceed the assignment maximum.");

        row.Marks = request.Marks;
        row.Feedback = request.Feedback?.Trim();
        row.Status = SubmissionStatus.Graded;
        row.GradedAtUtc = DateTime.UtcNow;

        var existingMark = await db.Marks.SingleOrDefaultAsync(
            m => m.StudentId == row.StudentId &&
                 m.CourseId == row.Assignment.CourseId &&
                 m.Source == "Assignment" &&
                 m.Title == row.Assignment.Title,
            cancellationToken);

        if (existingMark is null)
        {
            db.Marks.Add(new Mark
            {
                StudentId = row.StudentId,
                CourseId = row.Assignment.CourseId,
                Source = "Assignment",
                Title = row.Assignment.Title,
                Score = request.Marks,
                MaximumScore = row.Assignment.MaximumMarks
            });
        }
        else
        {
            existingMark.Score = request.Marks;
            existingMark.MaximumScore = row.Assignment.MaximumMarks;
            existingMark.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        return MapSubmission(row);
    }

    // Creates a quiz and assigns it to the trainer-selected enrolled students.
    public async Task<QuizDto> CreateQuizAsync(
        int courseId, QuizRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Courses.AnyAsync(c => c.Id == courseId, cancellationToken))
            throw new LmsException(404, "Course not found.");

        if (request.Questions.Count == 0)
            throw new LmsException(400, "Add at least one question.");

        if (request.AssignedStudentIds is null ||
            request.AssignedStudentIds.Count == 0)
            throw new LmsException(400, "Select at least one student for this quiz.");

        if (request.Questions.Any(q =>
            q.Options.Count < 2 ||
            q.CorrectOptionIndex < 0 ||
            q.CorrectOptionIndex >= q.Options.Count))
            throw new LmsException(
                400, "Each question needs at least two options and a valid correct-option index.");

        if (request.Questions.Any(q => q.Marks <= 0))
            throw new LmsException(400, "Question marks must be greater than zero.");

        var selectedStudentIds = request.AssignedStudentIds.Distinct().ToList();

        if (selectedStudentIds.Any(id => id <= 0))
            throw new LmsException(400, "Student IDs must be positive numbers.");

        var enrolledStudentIds = await db.Enrollments
            .Where(e => e.CourseId == courseId &&
                        e.IsActive &&
                        selectedStudentIds.Contains(e.StudentId))
            .Select(e => e.StudentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (enrolledStudentIds.Count != selectedStudentIds.Count)
            throw new LmsException(
                400, "Every selected student must be actively enrolled in this course.");

        var totalMarks = request.Questions.Sum(q => q.Marks);

        if (request.PassingMarks < 0 || request.PassingMarks > totalMarks)
            throw new LmsException(
                400, "Passing marks must be between zero and the total question marks.");

        var quiz = new Quiz
        {
            CourseId = courseId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            TimeLimitMinutes = request.TimeLimitMinutes,
            PassingMarks = request.PassingMarks,
            IsPublished = request.Publish,
            IsAiGenerated = request.IsAiGenerated
        };

        foreach (var (input, questionIndex) in
                 request.Questions.Select((q, i) => (q, i)))
        {
            var question = new Question
            {
                QuestionText = input.Text.Trim(),
                Marks = input.Marks,
                Explanation = input.Explanation?.Trim(),
                Order = questionIndex + 1
            };

            question.Options = input.Options.Select((text, i) => new QuizOption
            {
                Text = text.Trim(),
                Label = ((char)('A' + i)).ToString(),
                IsCorrect = i == input.CorrectOptionIndex
            }).ToList();

            quiz.Questions.Add(question);
        }

        // Run the entire transaction through EF Core's retry strategy.
        var strategy = db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await db.Database.BeginTransactionAsync(cancellationToken);

            db.Quizzes.Add(quiz);
            await db.SaveChangesAsync(cancellationToken);

            foreach (var studentId in selectedStudentIds)
            {
                db.QuizAssignments.Add(new QuizAssignment
                {
                    QuizId = quiz.Id,
                    StudentId = studentId,
                    AssignedAtUtc = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        return MapQuiz(quiz, true);
    }


    public async Task<QuizDto?> GetQuizAsync(
        int quizId,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var quiz = await db.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
            .SingleOrDefaultAsync(
                q => q.Id == quizId,
                cancellationToken);

        if (quiz is null)
            return null;

        var isStaff = userId.HasValue &&
            await db.Users
                .Include(u => u.Role)
                .AnyAsync(
                    u => u.Id == userId.Value &&
                         (u.Role.Name == "Admin" ||
                          u.Role.Name == "Trainer"),
                    cancellationToken);

        if (isStaff)
            return MapQuiz(quiz, true);

        if (!quiz.IsPublished)
            throw new LmsException(404, "Quiz not found.");

        if (!userId.HasValue)
            throw new LmsException(401, "Authentication is required.");

        var studentId = await db.Students
            .Where(s => s.UserId == userId.Value)
            .Select(s => (int?)s.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (!studentId.HasValue)
            throw new LmsException(403, "Student profile not found.");

        var isEnrolled = await db.Enrollments.AnyAsync(
            e => e.CourseId == quiz.CourseId &&
                 e.StudentId == studentId.Value &&
                 e.IsActive,
            cancellationToken);

        if (!isEnrolled)
            throw new LmsException(
                403, "Enroll in the course to access this quiz.");

        var quizHasAssignments = await db.QuizAssignments
            .AnyAsync(
                a => a.QuizId == quizId,
                cancellationToken);

        if (quizHasAssignments)
        {
            var isAssigned = await db.QuizAssignments.AnyAsync(
                a => a.QuizId == quizId &&
                     a.StudentId == studentId.Value,
                cancellationToken);

            if (!isAssigned)
                throw new LmsException(
                    403, "This quiz has not been assigned to you.");
        }

        // For older quizzes without any assignment records,
        // active course enrollment is used as the fallback.
        // Correct answers remain hidden from students.
        return MapQuiz(quiz, false);
    }


    public async Task<bool> DeleteQuizAsync(
        int quizId, CancellationToken cancellationToken = default)
    {
        var quiz = await db.Quizzes
            .SingleOrDefaultAsync(q => q.Id == quizId, cancellationToken);

        if (quiz is null) return false;

        db.Quizzes.Remove(quiz);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<QuizAttemptResult> SubmitQuizAsync(
        int quizId, int userId, QuizAttemptRequest request,
        CancellationToken cancellationToken = default)
    {
        var student = await db.Students
            .SingleOrDefaultAsync(s => s.UserId == userId, cancellationToken)
            ?? throw new LmsException(403, "Only students can take quizzes.");

        var quiz = await db.Quizzes.Include(q => q.Questions)
            .ThenInclude(q => q.Options)
            .SingleOrDefaultAsync(
                q => q.Id == quizId && q.IsPublished, cancellationToken)
            ?? throw new LmsException(404, "Published quiz not found.");

        if (!await db.Enrollments.AnyAsync(
            e => e.CourseId == quiz.CourseId &&
                 e.StudentId == student.Id && e.IsActive,
            cancellationToken))
            throw new LmsException(403, "Enroll in the course before taking this quiz.");

        if (!await db.QuizAssignments.AnyAsync(
            a => a.QuizId == quizId && a.StudentId == student.Id,
            cancellationToken))
            throw new LmsException(403, "This quiz has not been assigned to you.");

        if (await db.QuizAttempts.AnyAsync(
            a => a.QuizId == quizId && a.StudentId == student.Id,
            cancellationToken))
            throw new LmsException(
                409, "This quiz has already been submitted. Duplicate attempts are not allowed.");

        if (request.Answers.GroupBy(a => a.QuestionId).Any(g => g.Count() > 1))
            throw new LmsException(400, "Duplicate question answers are not allowed.");

        var questionById = quiz.Questions.ToDictionary(q => q.Id);

        foreach (var answer in request.Answers)
        {
            if (!questionById.TryGetValue(answer.QuestionId, out var question))
                throw new LmsException(
                    400, "An answer refers to a question outside this quiz.");

            if (answer.SelectedOptionId.HasValue &&
                !question.Options.Any(o => o.Id == answer.SelectedOptionId.Value))
                throw new LmsException(
                    400, "Selected option does not belong to its question.");
        }

        var answerByQuestion = request.Answers
            .ToDictionary(a => a.QuestionId, a => a.SelectedOptionId);

        var totalMarks = quiz.Questions.Sum(q => q.Marks);

        var attempt = new QuizAttempt
        {
            QuizId = quiz.Id,
            StudentId = student.Id,
            TotalMarks = totalMarks,
            StartedAtUtc = DateTime.UtcNow,
            SubmittedAtUtc = DateTime.UtcNow
        };

        var review = new List<AnswerReviewDto>();

        foreach (var question in quiz.Questions.OrderBy(q => q.Order))
        {
            answerByQuestion.TryGetValue(question.Id, out var selectedId);

            var selected = question.Options.FirstOrDefault(o => o.Id == selectedId);
            var correct = question.Options.First(o => o.IsCorrect);
            var isCorrect = QuizScoring.IsCorrect(question, selectedId);
            var marksAwarded = isCorrect ? question.Marks : 0;

            attempt.Score += marksAwarded;

            attempt.Answers.Add(new StudentAnswer
            {
                QuestionId = question.Id,
                SelectedOptionId = selectedId,
                IsCorrect = isCorrect,
                MarksAwarded = marksAwarded
            });

            review.Add(new AnswerReviewDto(
                question.Id,
                question.QuestionText,
                selected?.Text,
                correct.Text,
                isCorrect,
                marksAwarded,
                question.Explanation));
        }

        // Run the attempt and mark writes in one retriable transaction.
        var strategy = db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx =
                await db.Database.BeginTransactionAsync(cancellationToken);

            db.QuizAttempts.Add(attempt);

            db.Marks.Add(new Mark
            {
                CourseId = quiz.CourseId,
                StudentId = student.Id,
                Source = "Quiz",
                Title = quiz.Title,
                Score = attempt.Score,
                MaximumScore = totalMarks
            });

            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        });

        var percentage = totalMarks == 0
            ? 0
            : Math.Round(attempt.Score / totalMarks * 100, 1);

        return new QuizAttemptResult(
            attempt.Id,
            attempt.Score,
            totalMarks,
            percentage,
            attempt.Score >= quiz.PassingMarks,
            review);
    }

    public async Task<IReadOnlyList<MarkDto>> ListMarksAsync(
        int? userId, int? courseId,
        CancellationToken cancellationToken = default)
    {
        var query = db.Marks.AsNoTracking()
            .Include(m => m.Course)
            .Include(m => m.Student).ThenInclude(s => s.User)
            .AsQueryable();

        if (userId.HasValue)
        {
            var studentId = await db.Students
                .Where(s => s.UserId == userId.Value)
                .Select(s => (int?)s.Id)
                .SingleOrDefaultAsync(cancellationToken);

            if (studentId.HasValue)
                query = query.Where(m => m.StudentId == studentId.Value);
            else
                throw new LmsException(403, "Student profile not found.");
        }

        if (courseId.HasValue)
            query = query.Where(m => m.CourseId == courseId.Value);

        var rows = await query.OrderByDescending(m => m.CreatedAtUtc)
            .Take(1000).ToListAsync(cancellationToken);

        return rows.Select(MapMark).ToList();
    }

    public async Task<MarkDto> AddMarkAsync(
        MarkRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Score > request.MaximumScore)
            throw new LmsException(400, "Score cannot exceed maximum score.");

        var enrolled = await db.Enrollments.AnyAsync(
            e => e.CourseId == request.CourseId &&
                 e.StudentId == request.StudentId && e.IsActive,
            cancellationToken);

        if (!enrolled)
            throw new LmsException(
                400, "Student is not actively enrolled in the course.");

        var mark = new Mark
        {
            CourseId = request.CourseId,
            StudentId = request.StudentId,
            Title = request.Title.Trim(),
            Source = request.Source.Trim(),
            Score = request.Score,
            MaximumScore = request.MaximumScore
        };

        db.Marks.Add(mark);
        await db.SaveChangesAsync(cancellationToken);

        mark = await db.Marks.Include(m => m.Course)
            .Include(m => m.Student).ThenInclude(s => s.User)
            .SingleAsync(m => m.Id == mark.Id, cancellationToken);

        return MapMark(mark);
    }

    public async Task<bool> DeleteMarkAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var mark = await db.Marks
            .SingleOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (mark is null) return false;

        db.Marks.Remove(mark);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    internal static QuizDto MapQuiz(Quiz quiz, bool includeAnswers)
    {
        var questions = quiz.Questions.OrderBy(q => q.Order)
            .Select(q => new QuestionDto(
                q.Id,
                q.QuestionText,
                q.Marks,
                includeAnswers ? q.Explanation : null,
                q.Options.OrderBy(o => o.Label)
                    .Select(o => new QuizOptionDto(
                        o.Id,
                        o.Label,
                        o.Text,
                        includeAnswers ? o.IsCorrect : null))
                    .ToList(),
                includeAnswers
                    ? q.Options.FirstOrDefault(o => o.IsCorrect)?.Id
                    : null))
            .ToList();

        return new QuizDto(
            quiz.Id,
            quiz.CourseId,
            quiz.Title,
            quiz.Description,
            quiz.TimeLimitMinutes,
            quiz.PassingMarks,
            quiz.IsPublished,
            quiz.IsAiGenerated,
            questions);
    }

    private static AssignmentDto MapAssignment(
        Assignment assignment, Submission? submission) => new(
        assignment.Id,
        assignment.CourseId,
        assignment.Title,
        assignment.Description,
        assignment.DueAtUtc,
        assignment.MaximumMarks,
        assignment.Status.ToString(),
        submission is not null,
        submission?.Marks,
        submission?.Feedback,
        assignment.AttachmentUrl);

    private static SubmissionDto MapSubmission(Submission s) => new(
        s.Id,
        s.AssignmentId,
        s.Assignment.Title,
        s.StudentId,
        s.Student.User.Name,
        s.Text,
        s.FileUrl,
        s.Status.ToString(),
        s.Marks,
        s.Assignment.MaximumMarks,
        s.Feedback,
        s.SubmittedAtUtc);

    private static MarkDto MapMark(Mark m)
    {
        var percent = m.MaximumScore == 0
            ? 0
            : Math.Round(m.Score / m.MaximumScore * 100, 1);

        return new MarkDto(
            m.Id,
            m.CourseId,
            m.Course.Title,
            m.Student.User.Name,
            m.Source,
            m.Title,
            m.Score,
            m.MaximumScore,
            percent,
            ProgressCalculator.Grade(percent),
            m.CreatedAtUtc);
    }
}
