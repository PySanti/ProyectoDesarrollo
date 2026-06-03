using FluentValidation;

namespace Umbral.TeamService.Application.Teams.TransferLeadership;

public sealed class TransferirLiderazgoCommandValidator : AbstractValidator<TransferirLiderazgoCommand>
{
    public TransferirLiderazgoCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.NuevoLiderUserId).NotEmpty();
    }
}
