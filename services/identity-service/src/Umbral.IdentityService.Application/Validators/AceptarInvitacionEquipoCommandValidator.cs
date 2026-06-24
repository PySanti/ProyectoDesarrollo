using FluentValidation;
using Umbral.IdentityService.Application.Commands;

namespace Umbral.IdentityService.Application.Validators;

public sealed class AceptarInvitacionEquipoCommandValidator : AbstractValidator<AceptarInvitacionEquipoCommand>
{
    public AceptarInvitacionEquipoCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.InvitacionId).NotEmpty();
    }
}
