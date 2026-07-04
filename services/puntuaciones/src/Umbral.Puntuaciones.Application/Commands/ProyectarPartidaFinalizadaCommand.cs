using MediatR;

namespace Umbral.Puntuaciones.Application.Commands;

public sealed record ProyectarPartidaFinalizadaCommand(
    Guid EventId, DateTime OccurredAt, Guid PartidaId, Guid SesionPartidaId, DateTime FechaFin) : IRequest;
