using AI_LMS.Domain.Entities;
using AI_LMS.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;

namespace AI_LMS.Infrastructure.Data;

public sealed class DbInitializer(ApplicationDbContext db, AI_LMS.Application.Interfaces.IPasswordService passwords)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);
        if (await db.Roles.AnyAsync(cancellationToken)) return;

        var adminRole = new Role { Name = "Admin" };
        var trainerRole = new Role { Name = "Trainer" };
        var studentRole = new Role { Name = "Student" };
        db.Roles.AddRange(adminRole, trainerRole, studentRole);
        await db.SaveChangesAsync(cancellationToken);

        var admin = NewUser("AI-LMS Administrator", "admin@ailms.com", "admin123", adminRole);
        var trainerSpecs = new[]
        {
            ("Ravi Kumar", "trainer@ailms.com", "Full Stack, ASP.NET Core, SQL Server", "Enterprise full-stack developer and mentor."),
            ("Meera Nair", "meera@ailms.com", "React, TypeScript, Azure", "Frontend architect and cloud specialist."),
            ("John D'Souza", "john@ailms.com", "Python, AI and Machine Learning", "Data science and machine learning trainer.")
        };
        var trainers = trainerSpecs.Select(x =>
        {
            var user = NewUser(x.Item1, x.Item2, "trainer123", trainerRole);
            user.Trainer = new Trainer { User = user, Expertise = x.Item3, Bio = x.Item4 };
            return user;
        }).ToList();
        var studentSpecs = new[]
        {
            ("Arjun P", "student@ailms.com"), ("Divya S", "divya@ailms.com"), ("Karthik R", "karthik@ailms.com"),
            ("Priya M", "priya@ailms.com"), ("Rahul V", "rahul@ailms.com"), ("Ananya T", "ananya@ailms.com"),
            ("Vikram J", "vikram@ailms.com"), ("Sneha K", "sneha@ailms.com"), ("Manish B", "manish@ailms.com"),
            ("Fatima A", "fatima@ailms.com")
        };
        var students = studentSpecs.Select((x, i) =>
        {
            var user = NewUser(x.Item1, x.Item2, "student123", studentRole);
            user.Student = new Student { User = user, Department = i % 2 == 0 ? "Computer Science" : "Information Technology", JoinDateUtc = DateTime.UtcNow.AddDays(-30 - i) };
            return user;
        }).ToList();
        db.Users.Add(admin);
        db.Users.AddRange(trainers);
        db.Users.AddRange(students);
        await db.SaveChangesAsync(cancellationToken);

        var trainerIds = trainers.Select(x => x.Trainer!.Id).ToArray();
        var studentIds = students.Select(x => x.Student!.Id).ToArray();
        var courses = new List<Course>
        {
            new() { Title = "Full Stack Web Development", Description = "Build production-ready web apps with React, ASP.NET Core Web API, SQL Server, authentication and deployment.", Category = "Web Development", Level = "Beginner", DurationWeeks = 12, Fee = 15000, TrainerId = trainerIds[0], Status = Domain.Enums.CourseStatus.Active },
            new() { Title = "Database Design & SQL Server", Description = "Relational modelling, normalisation, joins, indexes, stored procedures and performance tuning.", Category = "Database", Level = "Intermediate", DurationWeeks = 6, Fee = 8000, TrainerId = trainerIds[0], Status = Domain.Enums.CourseStatus.Active },
            new() { Title = "Modern React & TypeScript", Description = "Build accessible single-page applications with components, hooks, routing and reliable API integration.", Category = "Frontend", Level = "Intermediate", DurationWeeks = 8, Fee = 10000, TrainerId = trainerIds[1], Status = Domain.Enums.CourseStatus.Active },
            new() { Title = "Cloud Fundamentals & DevOps", Description = "Learn containers, CI/CD, monitoring, Azure App Service and safe production releases.", Category = "Cloud", Level = "Advanced", DurationWeeks = 8, Fee = 12000, TrainerId = trainerIds[1], Status = Domain.Enums.CourseStatus.Active },
            new() { Title = "Introduction to AI & Machine Learning", Description = "Understand model training, evaluation and how to build responsible AI-powered applications.", Category = "Artificial Intelligence", Level = "Beginner", DurationWeeks = 10, Fee = 18000, TrainerId = trainerIds[2], Status = Domain.Enums.CourseStatus.Active }
        };
        db.Courses.AddRange(courses);
        await db.SaveChangesAsync(cancellationToken);

        var lessonTemplates = new[]
        {
            new[] { "Web fundamentals", "JavaScript and TypeScript", "React components and state", "REST APIs with ASP.NET Core", "SQL Server and EF Core", "Authentication and deployment" },
            new[] { "Relational modelling", "Normalisation", "SQL joins and subqueries", "Indexes and execution plans" },
            new[] { "Component architecture", "React hooks", "Routing and forms", "Testing and accessibility" },
            new[] { "Cloud concepts", "Containers and Docker", "CI/CD pipelines", "Azure application deployment" },
            new[] { "What is machine learning?", "Preparing data", "Training and evaluating models", "Responsible AI" }
        };
        var lessons = new List<Lesson>();
        for (var i = 0; i < courses.Count; i++)
        {
            var order = 0;
            foreach (var title in lessonTemplates[i])
            {
                order++;
                lessons.Add(new Lesson
                {
                    CourseId = courses[i].Id, Title = title, Order = order, DurationMinutes = 25 + order * 5,
                    Description = $"Learn the main ideas in {title.ToLowerInvariant()}.",
                    Content = $"{title} is an important part of {courses[i].Title}. Begin by understanding the vocabulary, then follow the examples and practise the concept in a small project. Review the key ideas, test your understanding and connect this lesson to the next step in the course."
                });
            }
        }
        db.Lessons.AddRange(lessons);
        await db.SaveChangesAsync(cancellationToken);

        var assignments = new[]
        {
            new Assignment { CourseId = courses[0].Id, LessonId = lessons.First(l => l.CourseId == courses[0].Id).Id, Title = "Build a responsive portfolio", Description = "Create a responsive portfolio site and submit the Git repository or deployed URL.", MaximumMarks = 100, DueAtUtc = DateTime.UtcNow.AddDays(10), Status = Domain.Enums.AssignmentStatus.Published },
            new Assignment { CourseId = courses[0].Id, Title = "Build a React task tracker", Description = "Create a small task tracker using React state and accessible controls.", MaximumMarks = 50, DueAtUtc = DateTime.UtcNow.AddDays(18), Status = Domain.Enums.AssignmentStatus.Published },
            new Assignment { CourseId = courses[1].Id, Title = "Normalise a data model", Description = "Convert the provided denormalised training records into a 3NF schema.", MaximumMarks = 100, DueAtUtc = DateTime.UtcNow.AddDays(12), Status = Domain.Enums.AssignmentStatus.Published },
            new Assignment { CourseId = courses[2].Id, Title = "Accessible component library", Description = "Build reusable Button, Card and Modal components with keyboard support.", MaximumMarks = 100, DueAtUtc = DateTime.UtcNow.AddDays(14), Status = Domain.Enums.AssignmentStatus.Published },
            new Assignment { CourseId = courses[4].Id, Title = "Evaluate a simple model", Description = "Compare accuracy and recall on a small labelled dataset and explain the trade-offs.", MaximumMarks = 100, DueAtUtc = DateTime.UtcNow.AddDays(21), Status = Domain.Enums.AssignmentStatus.Published }
        };
        db.Assignments.AddRange(assignments);

        var quiz = new Quiz { CourseId = courses[0].Id, Title = "Web Development Fundamentals", Description = "Check the core concepts from the first lessons.", TimeLimitMinutes = 20, PassingMarks = 2, IsPublished = true };
        var questions = new[]
        {
            MakeQuestion(quiz, "Which HTTP method is normally used to create a resource?", ["GET", "POST", "DELETE", "OPTIONS"], 1, "POST submits data to create a resource."),
            MakeQuestion(quiz, "What does a React component return?", ["A database row", "A user interface description", "An HTTP status code", "A SQL index"], 1, "A React component describes the UI to render."),
            MakeQuestion(quiz, "Which practice protects a stored password?", ["Plain-text storage", "Password hashing", "Putting it in a URL", "Logging it"], 1, "A password hash is stored instead of the raw password.")
        };
        quiz.Questions = questions;
        db.Quizzes.Add(quiz);
        await db.SaveChangesAsync(cancellationToken);

        var enrollPairs = new (int Course, int Student)[]
        {
            (0,0),(1,0),(0,1),(2,1),(2,2),(3,2),(4,3),(0,4),(1,5),(3,6),(4,7),(2,8),(4,9)
        };
        foreach (var (courseIndex, studentIndex) in enrollPairs)
            db.Enrollments.Add(new Enrollment { CourseId = courses[courseIndex].Id, StudentId = studentIds[studentIndex], IsActive = true });

        var firstCourseLessons = lessons.Where(l => l.CourseId == courses[0].Id).OrderBy(l => l.Order).Take(3).ToList();
        foreach (var lesson in firstCourseLessons)
            db.LessonProgress.Add(new LessonProgress { LessonId = lesson.Id, StudentId = studentIds[0], IsCompleted = true, CompletedAtUtc = DateTime.UtcNow.AddDays(-1) });
        db.Submissions.Add(new Submission { AssignmentId = assignments[0].Id, StudentId = studentIds[0], Text = "Portfolio repository: https://github.com/example/portfolio", Status = Domain.Enums.SubmissionStatus.Submitted, SubmittedAtUtc = DateTime.UtcNow.AddHours(-3) });
        db.Marks.Add(new Mark { CourseId = courses[0].Id, StudentId = studentIds[0], Source = "Assignment", Title = "Starter portfolio", Score = 82, MaximumScore = 100 });
        db.Attendance.AddRange(
            new Attendance { CourseId = courses[0].Id, StudentId = studentIds[0], Date = DateOnly.FromDateTime(DateTime.UtcNow), IsPresent = true, MarkedByUserId = trainers[0].Id },
            new Attendance { CourseId = courses[0].Id, StudentId = studentIds[1], Date = DateOnly.FromDateTime(DateTime.UtcNow), IsPresent = true, MarkedByUserId = trainers[0].Id });
        await db.SaveChangesAsync(cancellationToken);
    }

    private User NewUser(string name, string email, string password, Role role) => new()
    {
        Name = name, Email = email.ToLowerInvariant(), PasswordHash = passwords.Hash(password),
        Role = role, RoleId = role.Id, IsActive = true
    };

    private static Question MakeQuestion(Quiz quiz, string text, string[] options, int correctIndex, string explanation)
    {
        var question = new Question { Quiz = quiz, QuestionText = text, Marks = 1, Order = quiz.Questions.Count + 1, Explanation = explanation };
        question.Options = options.Select((option, index) => new QuizOption
        {
            Question = question, Label = ((char)('A' + index)).ToString(), Text = option, IsCorrect = index == correctIndex
        }).ToList();
        return question;
    }
}
