using MediatR;

namespace Umbral.Puntuaciones.Application.Commands;

public sealed record ProyectarConvocatoriaRespondidaCommand(
    Guid EventId, DateTime OccurredAt, Guid ConvocatoriaId, Guid UsuarioId,
    string EstadoConvocatoria) : IRequest;
