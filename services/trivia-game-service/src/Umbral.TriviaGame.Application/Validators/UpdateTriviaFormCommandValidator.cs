using FluentValidation;
using Umbral.TriviaGame.Application.Commands;

namespace Umbral.TriviaGame.Application.Validators;

public sealed class UpdateTriviaFormCommandValidator : AbstractValidator<UpdateTriviaFormCommand>
{
    public UpdateTriviaFormCommandValidator()
    {
        RuleFor(x => x.FormId)
            .NotEmpty().WithMessage("El identificador del formulario es obligatorio.");

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
