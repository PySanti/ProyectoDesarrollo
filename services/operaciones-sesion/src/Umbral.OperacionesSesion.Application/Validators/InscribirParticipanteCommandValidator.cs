using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;

namespace Umbral.OperacionesSesion.Application.Validators;

public sealed class InscribirParticipanteCommandValidator : AbstractValidator<InscribirParticipanteCommand>
{
    public InscribirParticipanteCommandValidator()
    {
        RuleFor(x => x.PartidaId).NotEmpty();
        RuleFor(x => x.ParticipanteId).NotEmpty();
    }
}
