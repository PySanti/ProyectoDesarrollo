using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Commands;

public sealed record TransferirLiderazgoCommand(Guid ActorUserId, Guid NuevoLiderUserId) : IRequest<TransferirLiderazgoResponse>;
