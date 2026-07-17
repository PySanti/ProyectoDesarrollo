using FluentValidation;
using Umbral.IdentityService.Application.Commands;

namespace Umbral.IdentityService.Application.Validators;

public sealed class CreateUserWithInitialRoleCommandValidator : AbstractValidator<CreateUserWithInitialRoleCommand>
{
    public CreateUserWithInitialRoleCommandValidator()
    {
        RuleFor(x => x.Name).Cascade(CascadeMode.Stop).TextoHumano(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.InitialRole)
            .NotEmpty()
            .Must(role => role is "Administrador" or "Operador" or "Participante");
    }
}
