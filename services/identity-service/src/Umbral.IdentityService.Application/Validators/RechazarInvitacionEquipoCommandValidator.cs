using FluentValidation;
using Umbral.IdentityService.Application.Commands;

namespace Umbral.IdentityService.Application.Validators;

public sealed class RechazarInvitacionEquipoCommandValidator : AbstractValidator<RechazarInvitacionEquipoCommand>
{
    public RechazarInvitacionEquipoCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.InvitacionId).NotEmpty();
    }
}
