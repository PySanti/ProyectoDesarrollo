using MediatR;
using Umbral.IdentityService.Application.Users.Common;

namespace Umbral.IdentityService.Application.Users.GetUserById;

public sealed record GetUserByIdQuery(Guid UserId) : IRequest<UserDetailResponse>;
