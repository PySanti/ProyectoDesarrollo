using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;
namespace Umbral.OperacionesSesion.Application.Validators;
public sealed class ResponderPreguntaCommandValidator : AbstractValidator<ResponderPreguntaCommand>
{
    public ResponderPreguntaCommandValidator()
    {
        RuleFor(x => x.PartidaId).NotEmpty();
        RuleFor(x => x.OpcionId).NotEmpty();
    }
}
