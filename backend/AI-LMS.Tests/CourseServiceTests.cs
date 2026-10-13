using AI_LMS.Application.DTOs;
using AI_LMS.Application.Exceptions;
using AI_LMS.Domain.Entities;
using AI_LMS.Infrastructure.Services;
using Xunit;

namespace AI_LMS.Tests;

public sealed class CourseServiceTests
{
    [Fact]
    public async Task Admin_creates_updates_and_deletes_a_course()
    {
        await using var db = TestDbFactory.Create();
        var adminRole = new Role { Name = "Admin" };
        var admin = new User { Name = "Admin", Email = "admin@example.com", PasswordHash = "hash", Role = adminRole };
        db.Users.Add(admin);
        await db.SaveChangesAsync();
        var service = new CourseService(db);
        var created = await service.CreateAsync(Request("First course"), admin.Id);
        Assert.Equal("First course", created.Title);

        var updated = await service.UpdateAsync(created.Id, Request("Updated course"), admin.Id, true);
        Assert.Equal("Updated course", updated!.Title);
        Assert.True(await service.DeleteAsync(created.Id, admin.Id, true));
        Assert.Empty(db.Courses);
    }

    [Fact]
    public async Task Trainer_cannot_modify_a_course_owned_by_another_trainer()
    {
        await using var db = TestDbFactory.Create();
        var role = new Role { Name = "Trainer" };
        var owner = new User { Name = "Owner", Email = "owner@example.com", PasswordHash = "hash", Role = role };
        owner.Trainer = new Trainer { User = owner, Expertise = "SQL" };
        var outsider = new User { Name = "Other", Email = "other@example.com", PasswordHash = "hash", Role = role };
        outsider.Trainer = new Trainer { User = outsider, Expertise = "React" };
        db.Users.AddRange(owner, outsider);
        await db.SaveChangesAsync();
        var course = new Course { Title = "Owned course", Description = "A course description", Category = "Web", Level = "Beginner", TrainerId = owner.Trainer!.Id };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var service = new CourseService(db);
        await Assert.ThrowsAsync<LmsException>(() => service.UpdateAsync(course.Id, Request("Tamper"), outsider.Id, false));
        await Assert.ThrowsAsync<LmsException>(() => service.DeleteAsync(course.Id, outsider.Id, false));
    }

    [Fact]
    public async Task Student_can_enroll_once_and_duplicate_is_conflict()
    {
        await using var db = TestDbFactory.Create();
        var role = new Role { Name = "Student" };
        var user = new User { Name = "Learner", Email = "student@example.com", PasswordHash = "hash", Role = role };
        user.Student = new Student { User = user, Department = "CS" };
        var course = new Course { Title = "Course", Description = "Course description", Category = "Web", Level = "Beginner" };
        db.Users.Add(user);
        db.Courses.Add(course);
        await db.SaveChangesAsync();
        var service = new CourseService(db);

        await service.EnrollAsync(course.Id, user.Id);
        Assert.Single(db.Enrollments);
        await Assert.ThrowsAsync<LmsException>(() => service.EnrollAsync(course.Id, user.Id));
    }

    private static CourseRequest Request(string title) => new()
    {
        Title = title, Description = "A sufficiently long course description.", Category = "Web", Level = "Beginner", DurationWeeks = 4
    };
}
