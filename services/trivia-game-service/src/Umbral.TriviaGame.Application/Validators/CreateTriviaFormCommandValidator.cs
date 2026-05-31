using FluentValidation;
using Umbral.TriviaGame.Application.Commands;
using Umbral.TriviaGame.Application.Dtos;

namespace Umbral.TriviaGame.Application.Validators;

public sealed class CreateTriviaFormCommandValidator : AbstractValidator<CreateTriviaFormCommand>
{
    public CreateTriviaFormCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("El título del formulario es obligatorio.")
            .MaximumLength(200).WithMessage("El título del formulario no puede exceder 200 caracteres.");

        RuleFor(x => x.Questions)
            .NotNull().WithMessage("La lista de preguntas es obligatoria.");

        RuleFor(x => x.Questions)
            .Must(q => q.Count > 0).WithMessage("El formulario debe contener al menos una pregunta.")
            .When(x => x.Questions is not null);

        RuleForEach(x => x.Questions)
            .SetValidator(new QuestionInputValidator());
    }
}

internal sealed class QuestionInputValidator : AbstractValidator<QuestionInputDto>
{
    public QuestionInputValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("El texto de la pregunta es obligatorio.")
            .MaximumLength(1000).WithMessage("El texto de la pregunta no puede exceder 1000 caracteres.");

        RuleFor(x => x.AssignedScore)
            .InclusiveBetween(1, 1000)
            .WithMessage("El puntaje asignado debe estar entre 1 y 1000.");

        RuleFor(x => x.TimeLimitSeconds)
            .InclusiveBetween(5, 300)
            .WithMessage("El tiempo límite debe estar entre 5 y 300 segundos.");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(1)
            .WithMessage("El orden de visualización debe ser mayor o igual a 1.");

        RuleFor(x => x.Options)
            .NotNull().WithMessage("Las opciones de la pregunta son obligatorias.");

        RuleFor(x => x.Options.Count)
            .Equal(4).WithMessage("Cada pregunta debe tener exactamente 4 opciones.")
            .When(x => x.Options is not null);

        RuleForEach(x => x.Options)
            .SetValidator(new AnswerOptionInputValidator());

        RuleFor(x => x.Options)
            .Must(opt => opt is not null && opt.Count(o => o.IsCorrect) == 1)
            .WithMessage("Cada pregunta debe tener exactamente 1 opción correcta.")
            .When(x => x.Options is not null);
    }
}

internal sealed class AnswerOptionInputValidator : AbstractValidator<AnswerOptionInputDto>
{
    public AnswerOptionInputValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("El texto de la opción es obligatorio.")
            .MaximumLength(500).WithMessage("El texto de la opción no puede exceder 500 caracteres.");
    }
}
