using MediatR;

namespace Umbral.OperacionesSesion.Application.Commands;

public sealed record CancelarInscripcionEquipoCommand(Guid PartidaId, Guid LiderId, string? BearerToken)
    : IRequest;
