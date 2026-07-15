using FluentValidation;
using Umbral.IdentityService.Application.Commands;

namespace Umbral.IdentityService.Application.Validators;

public sealed class CambiarEstadoEquipoAdminCommandValidator : AbstractValidator<CambiarEstadoEquipoAdminCommand>
{
    private static readonly string[] EstadosPermitidos = { "Desactivado", "Activo" };

    public CambiarEstadoEquipoAdminCommandValidator()
    {
        RuleFor(x => x.EquipoId)
            .NotEmpty();

        RuleFor(x => x.Estado)
            .Must(estado => EstadosPermitidos.Contains(estado))
            .WithMessage("Estado inválido: debe ser 'Desactivado' o 'Activo'.");
    }
}
