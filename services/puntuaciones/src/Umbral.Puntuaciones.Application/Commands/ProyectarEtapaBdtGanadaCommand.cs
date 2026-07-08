using MediatR;

namespace Umbral.Puntuaciones.Application.Commands;

public sealed record ProyectarEtapaBdtGanadaCommand(
    Guid EventId, DateTime OccurredAt, Guid PartidaId, Guid SesionPartidaId, Guid JuegoId,
    Guid EtapaId, Guid ParticipanteId, int Puntaje, long TiempoResolucionMs, Guid? EquipoId) : IRequest;
