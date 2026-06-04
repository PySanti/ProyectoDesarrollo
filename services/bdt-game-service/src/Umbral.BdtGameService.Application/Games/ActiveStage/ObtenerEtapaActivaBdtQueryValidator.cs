using FluentValidation;

namespace Umbral.BdtGameService.Application.Games.ActiveStage;

public sealed class ObtenerEtapaActivaBdtQueryValidator : AbstractValidator<ObtenerEtapaActivaBdtQuery>
{
    public ObtenerEtapaActivaBdtQueryValidator()
    {
        RuleFor(query => query.PartidaId).NotEmpty();
        RuleFor(query => query.ParticipanteUserId).NotEmpty();
    }
}
