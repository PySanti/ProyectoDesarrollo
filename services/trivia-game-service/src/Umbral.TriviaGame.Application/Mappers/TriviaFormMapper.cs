using Umbral.TriviaGame.Application.Dtos;
using Umbral.TriviaGame.Domain.DomainServices;
using Umbral.TriviaGame.Domain.Drafts;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Mappers;

internal static class TriviaFormMapper
{
    public static TriviaFormDetailDto ToDto(TriviaForm form)
    {
        var questions = form.Questions
            .OrderBy(q => q.DisplayOrder)
            .Select(q => new QuestionDetailDto(
                q.Id.Value,
                q.Text.Value,
                q.AssignedScore.Value,
                q.TimeLimit.Seconds,
                q.DisplayOrder,
                q.Options.Select(opt =>
                    new AnswerOptionDetailDto(
                        opt.Orden,
                        opt.Text.Value,
                        opt.IsCorrect)).ToList()))
            .ToList();

        return new TriviaFormDetailDto(
            form.Id.Value,
            form.Title.Value,
            form.IsComplete,
            TriviaFormCompletenessValidator.GetIncompleteReasons(form).ToList(),
            form.CreatedAtUtc,
            form.UpdatedAtUtc,
            questions);
    }

    public static List<QuestionDraft> ToDrafts(IReadOnlyList<QuestionInputDto> questionInputs)
    {
        return questionInputs
            .Select(q =>
            {
                var optionDrafts = q.Options
                    .Select(o => AnswerOptionDraft.Create(o.Text, o.IsCorrect))
                    .ToList();

                return QuestionDraft.Create(
                    q.Text,
                    q.AssignedScore,
                    q.TimeLimitSeconds,
                    q.DisplayOrder,
                    optionDrafts);
            })
            .ToList();
    }
}
