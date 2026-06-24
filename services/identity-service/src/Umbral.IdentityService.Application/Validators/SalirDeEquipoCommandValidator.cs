using FluentValidation;
using Umbral.IdentityService.Application.Commands;

namespace Umbral.IdentityService.Application.Validators;

public sealed class SalirDeEquipoCommandValidator : AbstractValidator<SalirDeEquipoCommand>
{
    public SalirDeEquipoCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}
