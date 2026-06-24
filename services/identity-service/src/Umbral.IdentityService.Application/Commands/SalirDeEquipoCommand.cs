using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Commands;

public sealed record SalirDeEquipoCommand(Guid ActorUserId) : IRequest<SalirDeEquipoResponse>;
