using MediatR;

namespace Umbral.Puntuaciones.Application.Commands;

public sealed record ProyectarInscripcionAceptadaCommand(
    Guid EventId, DateTime OccurredAt, Guid PartidaId, string Modalidad,
    Guid? ParticipanteId, Guid? EquipoId) : IRequest;
