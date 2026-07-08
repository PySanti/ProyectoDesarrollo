using FluentValidation;
using Umbral.IdentityService.Application.Commands;

namespace Umbral.IdentityService.Application.Validators;

public sealed class CrearEquipoAdminCommandValidator : AbstractValidator<CrearEquipoAdminCommand>
{
    public CrearEquipoAdminCommandValidator()
    {
        RuleFor(x => x.NombreEquipo)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(x => x.LiderUserId)
            .NotEmpty();
    }
}
