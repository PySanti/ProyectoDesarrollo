using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Commands;

public sealed record EliminarMiEquipoCommand(Guid ActorUserId) : IRequest<EliminarEquipoResponse>;
