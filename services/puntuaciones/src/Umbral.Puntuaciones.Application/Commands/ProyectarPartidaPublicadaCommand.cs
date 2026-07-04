using MediatR;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Application.Commands;

public sealed record ProyectarPartidaPublicadaCommand(
    Guid EventId, DateTime OccurredAt, Guid PartidaId, Guid SesionPartidaId, Modalidad Modalidad) : IRequest;
