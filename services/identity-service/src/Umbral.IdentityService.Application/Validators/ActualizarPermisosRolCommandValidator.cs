using FluentValidation;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.Application.Validators;

public sealed class ActualizarPermisosRolCommandValidator : AbstractValidator<ActualizarPermisosRolCommand>
{
    public ActualizarPermisosRolCommandValidator()
    {
        RuleFor(c => c.Rol)
            .Must(rol => Enum.TryParse<RolUsuario>(rol, ignoreCase: false, out _))
            .WithMessage("Rol inválido: debe ser Administrador, Operador o Participante.");

        RuleFor(c => c.Permisos).NotNull();

        RuleForEach(c => c.Permisos)
            .Must(p => Enum.TryParse<PermisoFuncional>(p, ignoreCase: false, out var permiso)
                       && PermisosGobernables.Todos.Contains(permiso))
            .WithMessage("Permiso inválido: el panel solo gobierna GestionarPartidas y GestionarEquipos.");
    }
}
