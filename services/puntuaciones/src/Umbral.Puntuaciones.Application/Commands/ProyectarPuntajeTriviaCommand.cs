using MediatR;

namespace Umbral.Puntuaciones.Application.Commands;

public sealed record ProyectarPuntajeTriviaCommand(
    Guid EventId, DateTime OccurredAt, Guid PartidaId, Guid SesionPartidaId, Guid JuegoId,
    Guid PreguntaId, Guid ParticipanteId, int Puntaje, long TiempoRespuestaMs, Guid? EquipoId) : IRequest;
