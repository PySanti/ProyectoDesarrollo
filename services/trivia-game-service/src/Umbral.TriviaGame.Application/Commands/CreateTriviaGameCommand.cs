using MediatR;
using Umbral.TriviaGame.Application.Dtos;

namespace Umbral.TriviaGame.Application.Commands;

public sealed record CreateTriviaGameCommand(
    string Nombre,
    string Modalidad,
    string ModoInicio,
    Guid FormularioId,
    DateTimeOffset TiempoInicio,
    int MinimoParticipantes,
    int? MaximoJugadores,
    int? MaximoEquipos,
    int? MinimoJugadoresPorEquipo,
    int? MaximoJugadoresPorEquipo
) : IRequest<TriviaGameDetailDto>;
