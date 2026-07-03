using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Commands;

public sealed record PreinscribirEquipoCommand(Guid PartidaId, Guid LiderId, string? BearerToken)
    : IRequest<PreinscripcionEquipoResponse>;
