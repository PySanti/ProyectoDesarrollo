using MediatR;
using Umbral.IdentityService.Application.Users.Common;

namespace Umbral.IdentityService.Application.Users.GetUsers;

public sealed record GetUsersQuery : IRequest<IReadOnlyList<UserSummaryResponse>>;
