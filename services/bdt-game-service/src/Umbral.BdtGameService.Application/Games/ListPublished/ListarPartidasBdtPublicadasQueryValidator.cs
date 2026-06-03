using FluentValidation;

namespace Umbral.BdtGameService.Application.Games.ListPublished;

public sealed class ListarPartidasBdtPublicadasQueryValidator : AbstractValidator<ListarPartidasBdtPublicadasQuery>
{
    public ListarPartidasBdtPublicadasQueryValidator()
    {
        RuleFor(query => query.ActorUserId)
            .NotEmpty();

        RuleFor(query => query.Modalidad)
            .Must(ModalidadFilterParser.IsValid)
            .WithMessage("La modalidad debe ser Individual o Equipo.");
    }
}
