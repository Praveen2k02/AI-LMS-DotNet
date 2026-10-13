using AI_LMS.Domain.Services;
using Xunit;

namespace AI_LMS.Tests;

public sealed class ProgressCalculatorTests
{
    [Fact]
    public void Calculates_the_documented_weighted_example()
    {
        Assert.Equal(88m, ProgressCalculator.CourseCompletion(15, 20, 4, 4, 2, 2));
    }

    [Fact]
    public void Complete_work_gives_one_hundred_percent()
    {
        Assert.Equal(100m, ProgressCalculator.CourseCompletion(8, 8, 3, 3, 1, 1));
    }

    [Fact]
    public void Missing_components_are_renormalised()
    {
        Assert.Equal(84m, ProgressCalculator.CourseCompletion(15, 20, 2, 2, 0, 0));
    }

    [Fact]
    public void Empty_course_has_zero_progress()
    {
        Assert.Equal(0m, ProgressCalculator.CourseCompletion(0, 0, 0, 0, 0, 0));
    }

    [Theory]
    [InlineData(100, "A+")]
    [InlineData(84, "A")]
    [InlineData(73, "B")]
    [InlineData(65, "C")]
    [InlineData(45, "D")]
    [InlineData(20, "F")]
    public void Maps_percentage_to_grade(decimal percentage, string expected) =>
        Assert.Equal(expected, ProgressCalculator.Grade(percentage));
}
