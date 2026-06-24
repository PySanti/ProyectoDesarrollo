using FluentValidation;
using Umbral.IdentityService.Application.Commands;

namespace Umbral.IdentityService.Application.Validators;

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
