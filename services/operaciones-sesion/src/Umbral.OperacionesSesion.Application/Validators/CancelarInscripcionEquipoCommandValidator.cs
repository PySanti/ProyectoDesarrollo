using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;

namespace Umbral.OperacionesSesion.Application.Validators;

public sealed class CancelarInscripcionEquipoCommandValidator : AbstractValidator<CancelarInscripcionEquipoCommand>
{
    public CancelarInscripcionEquipoCommandValidator()
    {
        RuleFor(x => x.PartidaId).NotEmpty();
        RuleFor(x => x.LiderId).NotEmpty();
    }
}
