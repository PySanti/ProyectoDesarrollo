using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;
namespace Umbral.OperacionesSesion.Application.Validators;
public sealed class EnviarPistaCommandValidator : AbstractValidator<EnviarPistaCommand>
{
    public EnviarPistaCommandValidator()
    {
        RuleFor(x => x.PartidaId).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.ParticipanteDestinoId.HasValue ^ x.EquipoDestinoId.HasValue)
            .WithMessage("Debe indicarse exactamente un destino: participanteDestinoId o equipoDestinoId.");
        RuleFor(x => x.ParticipanteDestinoId).NotEqual(Guid.Empty).When(x => x.ParticipanteDestinoId.HasValue);
        RuleFor(x => x.EquipoDestinoId).NotEqual(Guid.Empty).When(x => x.EquipoDestinoId.HasValue);
        RuleFor(x => x.Texto).NotEmpty().MaximumLength(500);
    }
}
