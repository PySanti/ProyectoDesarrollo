using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;

namespace Umbral.OperacionesSesion.Application.Validators;

public sealed class CancelarInscripcionCommandValidator : AbstractValidator<CancelarInscripcionCommand>
{
    public CancelarInscripcionCommandValidator()
    {
        RuleFor(x => x.PartidaId).NotEmpty();
        RuleFor(x => x.ParticipanteId).NotEmpty();
    }
}
