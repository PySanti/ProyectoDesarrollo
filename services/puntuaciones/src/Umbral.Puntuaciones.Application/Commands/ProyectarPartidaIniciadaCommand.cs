using MediatR;

namespace Umbral.Puntuaciones.Application.Commands;

public sealed record ProyectarPartidaIniciadaCommand(
    Guid EventId, DateTime OccurredAt, Guid PartidaId, Guid SesionPartidaId, DateTime FechaInicio) : IRequest;
