using FluentValidation;

namespace Umbral.BdtGameService.Application.Games.ListPublished;

public sealed class ListarPartidasBdtPublicadasOperadorQueryValidator
    : AbstractValidator<ListarPartidasBdtPublicadasOperadorQuery>
{
    public ListarPartidasBdtPublicadasOperadorQueryValidator()
    {
        RuleFor(query => query.ActorUserId)
            .NotEmpty();
    }
}
