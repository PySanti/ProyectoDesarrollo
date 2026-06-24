using FluentValidation;
using Umbral.IdentityService.Application.Commands;

namespace Umbral.IdentityService.Application.Validators;

public sealed class TransferirLiderazgoCommandValidator : AbstractValidator<TransferirLiderazgoCommand>
{
    public TransferirLiderazgoCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.NuevoLiderUserId).NotEmpty();
    }
}
