using FluentValidation;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.Application.Validators;

public sealed class CambiarRolUsuarioCommandValidator : AbstractValidator<CambiarRolUsuarioCommand>
{
    public CambiarRolUsuarioCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Rol)
            .Must(rol => Enum.TryParse<RolUsuario>(rol, ignoreCase: false, out _))
            .WithMessage("Rol inválido: debe ser Administrador, Operador o Participante.");
    }
}
