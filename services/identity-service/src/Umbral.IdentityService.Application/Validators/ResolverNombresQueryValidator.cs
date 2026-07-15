using FluentValidation;
using Umbral.IdentityService.Application.Queries;

namespace Umbral.IdentityService.Application.Validators;

public sealed class ResolverNombresQueryValidator : AbstractValidator<ResolverNombresQuery>
{
    public const int MaxIds = 200;

    public ResolverNombresQueryValidator()
    {
        RuleFor(q => q)
            .Must(q => q.ParticipanteIds.Count + q.EquipoIds.Count <= MaxIds)
            .OverridePropertyName("ids")
            .WithMessage($"El lote no puede superar {MaxIds} ids entre participanteIds y equipoIds.");
    }
}
