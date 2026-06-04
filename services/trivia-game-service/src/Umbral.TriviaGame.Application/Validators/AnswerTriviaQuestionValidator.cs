using FluentValidation;
using Umbral.TriviaGame.Application.Commands;

namespace Umbral.TriviaGame.Application.Validators;

public sealed class AnswerTriviaQuestionValidator : AbstractValidator<AnswerTriviaQuestionCommand>
{
    public AnswerTriviaQuestionValidator()
    {
        RuleFor(x => x.PartidaId)
            .NotEmpty().WithMessage("El identificador de la partida es obligatorio.");

        RuleFor(x => x.PreguntaId)
            .NotEmpty().WithMessage("El identificador de la pregunta es obligatorio.");

        RuleFor(x => x.UsuarioId)
            .NotEmpty().WithMessage("El identificador del usuario es obligatorio.");

        RuleFor(x => x.OpcionIndex)
            .InclusiveBetween(0, 3).WithMessage("El índice de la opción seleccionada debe estar entre 0 y 3.");
    }
}
