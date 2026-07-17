using FluentValidation;
using Umbral.IdentityService.Application.Commands;

namespace Umbral.IdentityService.Application.Validators;

public sealed class ReasignarLiderazgoAdminCommandValidator : AbstractValidator<ReasignarLiderazgoAdminCommand>
{
    public ReasignarLiderazgoAdminCommandValidator()
    {
        RuleFor(x => x.EquipoId)
            .NotEmpty();

        RuleFor(x => x.NuevoLiderUserId)
            .NotEmpty();
    }
}
