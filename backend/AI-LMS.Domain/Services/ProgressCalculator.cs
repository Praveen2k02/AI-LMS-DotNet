
namespace AI_LMS.Domain.Services;

public static class ProgressCalculator
{
    public static decimal Percent(int completed, int total) =>
        total <= 0
            ? 0
            : Math.Round(
                (decimal)completed / total * 100,
                MidpointRounding.AwayFromZero);

    public static decimal CourseCompletion(
        int completedLessons,
        int totalLessons,
        int submittedAssignments,
        int totalAssignments,
        int quizzesTaken,
        int totalQuizzes)
    {
        var components = new List<(decimal Value, decimal Weight)>();

        if (totalLessons > 0)
            components.Add((Percent(completedLessons, totalLessons), 0.5m));

        if (totalAssignments > 0)
            components.Add((Percent(submittedAssignments, totalAssignments), 0.3m));

        if (totalQuizzes > 0)
            components.Add((quizzesTaken > 0 ? 100m : 0m, 0.2m));

        var weights = components.Sum(x => x.Weight);

        return weights == 0
            ? 0
            : Math.Round(
                components.Sum(x => x.Value * x.Weight) / weights,
                MidpointRounding.AwayFromZero);
    }

    public static string Grade(decimal percentage) => percentage switch
    {
        >= 90 => "A+",
        >= 80 => "A",
        >= 70 => "B",
        >= 60 => "C",
        >= 40 => "D",
        _ => "F"
    };
}
