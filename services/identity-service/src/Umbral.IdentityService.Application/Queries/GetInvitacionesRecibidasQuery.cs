using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Queries;

public sealed record GetInvitacionesRecibidasQuery(Guid ActorUserId) : IRequest<IReadOnlyList<InvitacionRecibidasItemResponse>>;
