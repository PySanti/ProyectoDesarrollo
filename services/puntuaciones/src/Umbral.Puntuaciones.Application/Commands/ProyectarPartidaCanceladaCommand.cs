using MediatR;

namespace Umbral.Puntuaciones.Application.Commands;

public sealed record ProyectarPartidaCanceladaCommand(
    Guid EventId, DateTime OccurredAt, Guid PartidaId, Guid SesionPartidaId, DateTime FechaCancelacion) : IRequest;
