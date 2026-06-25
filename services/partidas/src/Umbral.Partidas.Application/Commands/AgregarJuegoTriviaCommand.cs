using MediatR;
using Umbral.Partidas.Application.DTOs;

namespace Umbral.Partidas.Application.Commands;

public sealed record AgregarJuegoTriviaCommand(
    Guid PartidaId,
    int Orden,
    IReadOnlyList<PreguntaRequest> Preguntas) : IRequest<AgregarJuegoResponse>;
