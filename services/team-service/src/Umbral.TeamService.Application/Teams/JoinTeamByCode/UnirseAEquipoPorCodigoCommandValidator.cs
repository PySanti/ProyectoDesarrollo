using FluentValidation;

namespace Umbral.TeamService.Application.Teams.JoinTeamByCode;

public sealed class UnirseAEquipoPorCodigoCommandValidator : AbstractValidator<UnirseAEquipoPorCodigoCommand>
{
    public UnirseAEquipoPorCodigoCommandValidator()
    {
        RuleFor(x => x.ActorUserId)
            .NotEmpty();

        RuleFor(x => x.CodigoAcceso)
            .NotEmpty()
            .MaximumLength(16);
    }
}
