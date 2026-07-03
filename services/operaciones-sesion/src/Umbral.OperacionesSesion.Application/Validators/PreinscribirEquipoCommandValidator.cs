using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;

namespace Umbral.OperacionesSesion.Application.Validators;

public sealed class PreinscribirEquipoCommandValidator : AbstractValidator<PreinscribirEquipoCommand>
{
    public PreinscribirEquipoCommandValidator()
    {
        RuleFor(x => x.PartidaId).NotEmpty();
        RuleFor(x => x.LiderId).NotEmpty();
    }
}
