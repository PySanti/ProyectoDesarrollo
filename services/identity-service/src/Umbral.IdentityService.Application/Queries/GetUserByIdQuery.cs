using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Queries;

public sealed record GetUserByIdQuery(Guid UserId) : IRequest<UserDetailResponse>;
