using FluentValidation;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Domain.Enums;

namespace Umbral.Partidas.Application.Validators;

public sealed class CrearPartidaCommandValidator : AbstractValidator<CrearPartidaCommand>
{
    public CrearPartidaCommandValidator()
    {
        RuleFor(x => x.NombrePartida).Cascade(CascadeMode.Stop).TextoHumano(120);
        RuleFor(x => x.Modalidad).IsInEnum();
        RuleFor(x => x.ModoInicioPartida).IsInEnum();
        RuleFor(x => x.MinimosParticipacion).GreaterThanOrEqualTo(1);
        RuleFor(x => x.MaximosParticipacion).GreaterThanOrEqualTo(x => x.MinimosParticipacion);

        When(x => x.ModoInicioPartida is ModoInicioPartida.Automatico or ModoInicioPartida.ManualYAutomatico, () =>
        {
            RuleFor(x => x.TiempoInicio).NotNull();
        });

        When(x => x.ModoInicioPartida == ModoInicioPartida.Manual, () =>
        {
            RuleFor(x => x.TiempoInicio).Null();
        });
    }
}
