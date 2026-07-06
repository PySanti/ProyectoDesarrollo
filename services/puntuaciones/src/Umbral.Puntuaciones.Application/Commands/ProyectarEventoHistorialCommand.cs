using MediatR;

namespace Umbral.Puntuaciones.Application.Commands;

// Comando genérico del historial (SP-4d): una fila por evento del contrato, ids extraídos por el
// mapper y el resto del payload resumido en DetalleJson.
public sealed record ProyectarEventoHistorialCommand(
    Guid EventId, string TipoEvento, DateTime OccurredAt, Guid PartidaId,
    Guid? JuegoId, Guid? ParticipanteId, Guid? EquipoId, string DetalleJson) : IRequest;
