using FluentValidation;

namespace Umbral.TeamService.Application.Teams.LeaveTeam;

public sealed class SalirDeEquipoCommandValidator : AbstractValidator<SalirDeEquipoCommand>
{
    public SalirDeEquipoCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}
