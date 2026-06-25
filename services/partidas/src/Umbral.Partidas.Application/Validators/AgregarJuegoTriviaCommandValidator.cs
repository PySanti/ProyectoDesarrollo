using FluentValidation;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Application.DTOs;

namespace Umbral.Partidas.Application.Validators;

public sealed class AgregarJuegoTriviaCommandValidator : AbstractValidator<AgregarJuegoTriviaCommand>
{
    public AgregarJuegoTriviaCommandValidator()
    {
        RuleFor(x => x.PartidaId).NotEmpty();
        RuleFor(x => x.Orden).GreaterThanOrEqualTo(1).WithMessage("El Orden debe ser mayor o igual a 1.");
        RuleFor(x => x.Preguntas).NotEmpty();
        RuleForEach(x => x.Preguntas).SetValidator(new PreguntaRequestValidator());
    }

    private sealed class PreguntaRequestValidator : AbstractValidator<PreguntaRequest>
    {
        public PreguntaRequestValidator()
        {
            RuleFor(p => p.Texto).NotEmpty();
            RuleFor(p => p.Puntaje).GreaterThan(0);
            RuleFor(p => p.TiempoLimiteSegundos).GreaterThan(0);
            RuleFor(p => p.Opciones).NotNull().Must(o => o is { Count: >= 2 })
                .WithMessage("Se requieren al menos 2 opciones.");
            RuleFor(p => p.Opciones).Must(o => o != null && o.Count(x => x.EsCorrecta) == 1)
                .WithMessage("Debe haber exactamente una opcion correcta.");
            RuleForEach(p => p.Opciones).ChildRules(o => o.RuleFor(x => x.Texto).NotEmpty());
        }
    }
}
