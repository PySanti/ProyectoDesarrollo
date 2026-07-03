using MediatR;

namespace Umbral.OperacionesSesion.Application.Commands;

public sealed record CancelarInscripcionCommand(Guid PartidaId, Guid ParticipanteId) : IRequest<Unit>;
