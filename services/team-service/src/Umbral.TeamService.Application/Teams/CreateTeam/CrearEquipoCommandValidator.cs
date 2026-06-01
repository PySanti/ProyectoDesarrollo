using FluentValidation;

namespace Umbral.TeamService.Application.Teams.CreateTeam;

public sealed class CrearEquipoCommandValidator : AbstractValidator<CrearEquipoCommand>
{
    public CrearEquipoCommandValidator()
    {
        RuleFor(x => x.ActorUserId)
            .NotEmpty();

        RuleFor(x => x.NombreEquipo)
            .NotEmpty()
            .MaximumLength(120);
    }
}
