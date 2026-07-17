using FluentValidation;
using Umbral.IdentityService.Application.Commands;

namespace Umbral.IdentityService.Application.Validators;

public sealed class RenombrarEquipoAdminCommandValidator : AbstractValidator<RenombrarEquipoAdminCommand>
{
    public RenombrarEquipoAdminCommandValidator()
    {
        RuleFor(x => x.EquipoId)
            .NotEmpty();

        RuleFor(x => x.NombreEquipo)
            .Cascade(CascadeMode.Stop)
            .TextoHumano(120);
    }
}
