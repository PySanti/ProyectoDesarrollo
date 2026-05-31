using FluentValidation;
using Umbral.TriviaGame.Application.Commands;

namespace Umbral.TriviaGame.Application.Validators;

public sealed class StartTriviaGameCommandValidator : AbstractValidator<StartTriviaGameCommand>
{
    public StartTriviaGameCommandValidator()
    {
        RuleFor(x => x.PartidaId)
            .NotEmpty().WithMessage("El identificador de la partida es obligatorio.");
    }
}
