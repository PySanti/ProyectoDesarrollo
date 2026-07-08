using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Queries;

public sealed record GetHistorialNombresEquipoQuery(Guid ActorUserId)
    : IRequest<HistorialNombresEquipoResponse>;
