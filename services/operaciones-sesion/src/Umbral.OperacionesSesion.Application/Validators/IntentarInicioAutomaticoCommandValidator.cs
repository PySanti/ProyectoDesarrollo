using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;

namespace Umbral.OperacionesSesion.Application.Validators;

public sealed class IntentarInicioAutomaticoCommandValidator : AbstractValidator<IntentarInicioAutomaticoCommand>
{
    public IntentarInicioAutomaticoCommandValidator()
    {
        RuleFor(x => x.PartidaId).NotEmpty();
    }
}
