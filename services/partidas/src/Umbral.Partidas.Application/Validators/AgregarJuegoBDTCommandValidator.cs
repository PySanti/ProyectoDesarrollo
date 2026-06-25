using FluentValidation;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Application.DTOs;

namespace Umbral.Partidas.Application.Validators;

public sealed class AgregarJuegoBDTCommandValidator : AbstractValidator<AgregarJuegoBDTCommand>
{
    public AgregarJuegoBDTCommandValidator()
    {
        RuleFor(x => x.PartidaId).NotEmpty();
        RuleFor(x => x.Orden).GreaterThanOrEqualTo(1);
        RuleFor(x => x.AreaBusqueda).NotEmpty();
        RuleFor(x => x.Etapas).NotEmpty();
        RuleForEach(x => x.Etapas).SetValidator(new EtapaRequestValidator());
    }

    private sealed class EtapaRequestValidator : AbstractValidator<EtapaRequest>
    {
        public EtapaRequestValidator()
        {
            RuleFor(e => e.Orden).GreaterThanOrEqualTo(1);
            RuleFor(e => e.CodigoQREsperado).NotEmpty();
            RuleFor(e => e.Puntaje).GreaterThan(0);
            RuleFor(e => e.TiempoLimiteSegundos).GreaterThan(0);
        }
    }
}
