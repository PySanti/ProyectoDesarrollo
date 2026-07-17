using MediatR;

namespace Umbral.Puntuaciones.Application.Commands;

public sealed record ProyectarConvocatoriaCreadaCommand(
    Guid EventId, DateTime OccurredAt, Guid PartidaId, Guid ConvocatoriaId,
    Guid EquipoId, Guid UsuarioId) : IRequest;
