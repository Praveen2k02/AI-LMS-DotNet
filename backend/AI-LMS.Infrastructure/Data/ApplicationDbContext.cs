
using AI_LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AI_LMS.Infrastructure.Data;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Trainer> Trainers => Set<Trainer>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonProgress> LessonProgress => Set<LessonProgress>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizAssignment> QuizAssignments => Set<QuizAssignment>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuizOption> QuizOptions => Set<QuizOption>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<StudentAnswer> StudentAnswers => Set<StudentAnswer>();
    public DbSet<Mark> Marks => Set<Mark>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<Attendance> Attendance => Set<Attendance>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);

        model.Entity<Role>(e =>
        {
            e.ToTable("Roles");
            e.Property(x => x.Name).HasMaxLength(30).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        model.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(254).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(30);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => new { x.RoleId, x.IsActive });

            e.HasOne(x => x.Role)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Student)
                .WithOne(x => x.User)
                .HasForeignKey<Student>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Trainer)
                .WithOne(x => x.User)
                .HasForeignKey<Trainer>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<Student>(e =>
        {
            e.ToTable("Students");
            e.Property(x => x.Department).HasMaxLength(120);
            e.HasIndex(x => x.UserId).IsUnique();
        });

        model.Entity<Trainer>(e =>
        {
            e.ToTable("Trainers");
            e.Property(x => x.Expertise).HasMaxLength(250);
            e.Property(x => x.Bio).HasMaxLength(2000);
            e.HasIndex(x => x.UserId).IsUnique();
        });

        model.Entity<Course>(e =>
        {
            e.ToTable("Courses");
            e.Property(x => x.Title).HasMaxLength(160).IsRequired();
            e.Property(x => x.Category).HasMaxLength(80).IsRequired();
            e.Property(x => x.Level).HasMaxLength(30).IsRequired();
            e.Property(x => x.Fee).HasPrecision(12, 2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => new { x.Status, x.Category, x.Level });

            e.HasOne(x => x.Trainer)
                .WithMany(x => x.Courses)
                .HasForeignKey(x => x.TrainerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        model.Entity<Enrollment>(e =>
        {
            e.ToTable("Enrollments");
            e.HasIndex(x => new { x.CourseId, x.StudentId }).IsUnique();

            e.HasOne(x => x.Course)
                .WithMany(x => x.Enrollments)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Student)
                .WithMany(x => x.Enrollments)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<Lesson>(e =>
        {
            e.ToTable("Lessons");
            e.Property(x => x.Title).HasMaxLength(160).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.HasIndex(x => new { x.CourseId, x.Order }).IsUnique();

            e.HasOne(x => x.Course)
                .WithMany(x => x.Lessons)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<LessonProgress>(e =>
        {
            e.ToTable("LessonProgress");
            e.HasIndex(x => new { x.StudentId, x.LessonId }).IsUnique();

            e.HasOne(x => x.Lesson)
                .WithMany(x => x.Progress)
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Student)
                .WithMany(x => x.LessonProgress)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<Assignment>(e =>
        {
            e.ToTable("Assignments");
            e.Property(x => x.Title).HasMaxLength(160).IsRequired();
            e.Property(x => x.MaximumMarks).HasPrecision(10, 2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

            e.HasOne(x => x.Course)
                .WithMany(x => x.Assignments)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(x => x.Lesson)
                .WithMany()
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        model.Entity<Submission>(e =>
        {
            e.ToTable("Submissions");
            e.Property(x => x.Marks).HasPrecision(10, 2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => new { x.AssignmentId, x.StudentId }).IsUnique();

            e.HasOne(x => x.Assignment)
                .WithMany(x => x.Submissions)
                .HasForeignKey(x => x.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<Quiz>(e =>
        {
            e.ToTable("Quizzes");
            e.Property(x => x.Title).HasMaxLength(160).IsRequired();
            e.Property(x => x.PassingMarks).HasPrecision(10, 2);

            e.HasOne(x => x.Course)
                .WithMany(x => x.Quizzes)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // NEW: Connect each quiz to its assigned students.
        model.Entity<QuizAssignment>(e =>
        {
            e.ToTable("QuizAssignments");

            e.HasIndex(x => new { x.QuizId, x.StudentId })
                .IsUnique();

            e.HasOne(x => x.Quiz)
                .WithMany(x => x.Assignments)
                .HasForeignKey(x => x.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        model.Entity<Question>(e =>
        {
            e.ToTable("Questions");
            e.Property(x => x.Marks).HasPrecision(8, 2);
            e.HasIndex(x => new { x.QuizId, x.Order });

            e.HasOne(x => x.Quiz)
                .WithMany(x => x.Questions)
                .HasForeignKey(x => x.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<QuizOption>(e =>
        {
            e.ToTable("QuizOptions");
            e.Property(x => x.Label).HasMaxLength(3).IsRequired();
            e.Property(x => x.Text).HasMaxLength(1000).IsRequired();

            e.HasOne(x => x.Question)
                .WithMany(x => x.Options)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<QuizAttempt>(e =>
        {
            e.ToTable("QuizAttempts");
            e.Property(x => x.Score).HasPrecision(10, 2);
            e.Property(x => x.TotalMarks).HasPrecision(10, 2);
            e.HasIndex(x => new { x.QuizId, x.StudentId }).IsUnique();

            e.HasOne(x => x.Quiz)
                .WithMany(x => x.Attempts)
                .HasForeignKey(x => x.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<StudentAnswer>(e =>
        {
            e.ToTable("StudentAnswers");
            e.Property(x => x.MarksAwarded).HasPrecision(8, 2);

            e.HasOne(x => x.QuizAttempt)
                .WithMany(x => x.Answers)
                .HasForeignKey(x => x.QuizAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Question)
                .WithMany()
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.SelectedOption)
                .WithMany()
                .HasForeignKey(x => x.SelectedOptionId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        model.Entity<Mark>(e =>
        {
            e.ToTable("Marks");
            e.Property(x => x.Source).HasMaxLength(30);
            e.Property(x => x.Score).HasPrecision(10, 2);
            e.Property(x => x.MaximumScore).HasPrecision(10, 2);
            e.HasIndex(x => new { x.StudentId, x.CourseId });

            e.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<Certificate>(e =>
        {
            e.ToTable("Certificates");
            e.Property(x => x.CertificateNumber).HasMaxLength(60).IsRequired();
            e.Property(x => x.VerificationCode).HasMaxLength(80).IsRequired();
            e.Property(x => x.FinalScore).HasPrecision(8, 2);
            e.HasIndex(x => x.VerificationCode).IsUnique();
            e.HasIndex(x => new { x.StudentId, x.CourseId }).IsUnique();

            e.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<Attendance>(e =>
        {
            e.ToTable("Attendance");
            e.HasIndex(x => new { x.CourseId, x.StudentId, x.Date }).IsUnique();

            e.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
