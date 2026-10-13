using AI_LMS.Domain.Entities;

namespace AI_LMS.Domain.Services;

public static class QuizScoring
{
    public static bool IsCorrect(Question question, int? selectedOptionId) =>
        selectedOptionId.HasValue && question.Options.Any(option => option.Id == selectedOptionId.Value && option.IsCorrect);

    public static decimal Score(IEnumerable<(Question Question, int? SelectedOptionId)> answers) =>
        answers.Sum(answer => IsCorrect(answer.Question, answer.SelectedOptionId) ? answer.Question.Marks : 0m);
}
