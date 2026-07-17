using FluentValidation;
using Umbral.OperacionesSesion.Application.Queries;

namespace Umbral.OperacionesSesion.Application.Validators;

// Mismo tope que /identity/directory/names para que el troceo del cliente movil sea
// identico en ambos directorios.
public sealed class ResolverNombresPartidaQueryValidator : AbstractValidator<ResolverNombresPartidaQuery>
{
    public const int MaxIds = 200;

    public ResolverNombresPartidaQueryValidator()
    {
        RuleFor(q => q.PartidaIds)
            .Must(ids => ids.Count <= MaxIds)
            .OverridePropertyName("partidaIds")
            .WithMessage($"El lote no puede superar {MaxIds} ids.");
    }
}
