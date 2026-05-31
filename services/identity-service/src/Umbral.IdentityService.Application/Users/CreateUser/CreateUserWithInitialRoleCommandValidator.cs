using FluentValidation;

namespace Umbral.IdentityService.Application.Users.CreateUser;

public sealed class CreateUserWithInitialRoleCommandValidator : AbstractValidator<CreateUserWithInitialRoleCommand>
{
    public CreateUserWithInitialRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.InitialRole)
            .NotEmpty()
            .Must(role => role is "Administrador" or "Operador" or "Participante");
    }
}
