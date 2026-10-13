using AI_LMS.Domain.Entities;
using AI_LMS.Domain.Services;
using Xunit;

namespace AI_LMS.Tests;

public sealed class QuizScoringTests
{
    [Fact]
    public void Scores_only_the_correct_option()
    {
        var q = Question(10, 3);
        Assert.True(QuizScoring.IsCorrect(q, 10));
        Assert.False(QuizScoring.IsCorrect(q, 11));
        Assert.False(QuizScoring.IsCorrect(q, null));
    }

    [Fact]
    public void Sum_of_correct_answers_uses_question_marks()
    {
        var a = Question(1, 2);
        var b = Question(2, 5);
        Assert.Equal(7m, QuizScoring.Score([(a, 1), (b, 2)]));
        Assert.Equal(2m, QuizScoring.Score([(a, 1), (b, 1)]));
    }

    private static Question Question(int correctOptionId, decimal marks) => new()
    {
        QuestionText = "Test question",
        Marks = marks,
        Options =
        [
            new QuizOption { Id = correctOptionId, Text = "Correct", Label = "A", IsCorrect = true },
            new QuizOption { Id = correctOptionId + 1, Text = "Incorrect", Label = "B", IsCorrect = false }
        ]
    };
}
