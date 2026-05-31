using FluentValidation;
using Umbral.TriviaGame.Application.Commands;

namespace Umbral.TriviaGame.Application.Validators;

public sealed class CreateTriviaGameCommandValidator : AbstractValidator<CreateTriviaGameCommand>
{
    public CreateTriviaGameCommandValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la partida es obligatorio.")
            .MinimumLength(3).WithMessage("El nombre debe tener al menos 3 caracteres.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.");

        RuleFor(x => x.Modalidad)
            .NotEmpty().WithMessage("La modalidad es obligatoria.")
            .Must(m => m is "Individual" or "Equipo")
            .WithMessage("La modalidad debe ser 'Individual' o 'Equipo'.");

        RuleFor(x => x.ModoInicio)
            .NotEmpty().WithMessage("El modo de inicio es obligatorio.")
            .Must(m => m is "Manual" or "Automatico")
            .WithMessage("El modo de inicio debe ser 'Manual' o 'Automatico'.");

        RuleFor(x => x.FormularioId)
            .NotEmpty().WithMessage("El identificador del formulario es obligatorio.");

        RuleFor(x => x.TiempoInicio)
            .NotEmpty().WithMessage("La fecha y hora de inicio son obligatorias.");

        RuleFor(x => x.MinimoParticipantes)
            .GreaterThanOrEqualTo(1)
            .WithMessage("La cantidad mínima de participantes debe ser mayor o igual a 1.");

        // --- Modalidad: Individual ---
        RuleFor(x => x.MaximoJugadores)
            .Must(x => x.HasValue).WithMessage("Debe especificar la cantidad máxima de jugadores.")
            .When(x => x.Modalidad == "Individual");

        RuleFor(x => x.MaximoJugadores)
            .GreaterThanOrEqualTo(x => x.MinimoParticipantes)
            .When(x => x.Modalidad == "Individual" && x.MaximoJugadores.HasValue);

        RuleFor(x => x.MaximoEquipos)
            .Must(x => x is null).WithMessage("No debe especificar cantidad máxima de equipos en modalidad Individual.")
            .When(x => x.Modalidad == "Individual");

        RuleFor(x => x.MinimoJugadoresPorEquipo)
            .Must(x => x is null).WithMessage("No debe especificar mínimo de jugadores por equipo en modalidad Individual.")
            .When(x => x.Modalidad == "Individual");

        RuleFor(x => x.MaximoJugadoresPorEquipo)
            .Must(x => x is null).WithMessage("No debe especificar máximo de jugadores por equipo en modalidad Individual.")
            .When(x => x.Modalidad == "Individual");

        // --- Modalidad: Equipo ---
        RuleFor(x => x.MaximoJugadores)
            .Must(x => x is null).WithMessage("No debe especificar cantidad máxima de jugadores en modalidad Equipo.")
            .When(x => x.Modalidad == "Equipo");

        RuleFor(x => x.MaximoEquipos)
            .Must(x => x.HasValue).WithMessage("Debe especificar la cantidad máxima de equipos.")
            .When(x => x.Modalidad == "Equipo");

        RuleFor(x => x.MaximoEquipos)
            .GreaterThanOrEqualTo(1)
            .When(x => x.Modalidad == "Equipo" && x.MaximoEquipos.HasValue);

        RuleFor(x => x.MinimoJugadoresPorEquipo)
            .Must(x => x.HasValue).WithMessage("Debe especificar el mínimo de jugadores por equipo.")
            .When(x => x.Modalidad == "Equipo");

        RuleFor(x => x.MinimoJugadoresPorEquipo)
            .GreaterThanOrEqualTo(1)
            .When(x => x.Modalidad == "Equipo" && x.MinimoJugadoresPorEquipo.HasValue);

        RuleFor(x => x.MaximoJugadoresPorEquipo)
            .Must(x => x.HasValue).WithMessage("Debe especificar el máximo de jugadores por equipo.")
            .When(x => x.Modalidad == "Equipo");

        RuleFor(x => x.MaximoJugadoresPorEquipo)
            .GreaterThanOrEqualTo(x => x.MinimoJugadoresPorEquipo ?? 1)
            .When(x => x.Modalidad == "Equipo" && x.MaximoJugadoresPorEquipo.HasValue);
    }
}
