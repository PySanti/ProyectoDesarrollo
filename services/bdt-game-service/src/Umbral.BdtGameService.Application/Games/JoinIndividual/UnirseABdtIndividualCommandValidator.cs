using FluentValidation;

namespace Umbral.BdtGameService.Application.Games.JoinIndividual;

public sealed class UnirseABdtIndividualCommandValidator : AbstractValidator<UnirseABdtIndividualCommand>
{
    public UnirseABdtIndividualCommandValidator()
    {
        RuleFor(command => command.PartidaId).NotEmpty();
        RuleFor(command => command.ParticipanteUserId).NotEmpty();
    }
}
