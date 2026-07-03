using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;

namespace Umbral.OperacionesSesion.Application.Validators;

public sealed class FinalizarJuegoActualCommandValidator : AbstractValidator<FinalizarJuegoActualCommand>
{
    public FinalizarJuegoActualCommandValidator()
    {
        RuleFor(x => x.PartidaId).NotEmpty();
    }
}
