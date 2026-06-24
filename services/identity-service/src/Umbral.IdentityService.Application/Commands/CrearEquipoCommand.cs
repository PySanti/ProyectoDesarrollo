using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Commands;

public sealed record CrearEquipoCommand(Guid ActorUserId, string NombreEquipo) : IRequest<CrearEquipoResponse>;
