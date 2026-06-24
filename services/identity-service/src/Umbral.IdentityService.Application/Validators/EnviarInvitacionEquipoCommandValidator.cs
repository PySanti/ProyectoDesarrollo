using FluentValidation;
using Umbral.IdentityService.Application.Commands;

namespace Umbral.IdentityService.Application.Validators;

public sealed class EnviarInvitacionEquipoCommandValidator : AbstractValidator<EnviarInvitacionEquipoCommand>
{
    public EnviarInvitacionEquipoCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.InvitadoUserId).NotEmpty();
    }
}
