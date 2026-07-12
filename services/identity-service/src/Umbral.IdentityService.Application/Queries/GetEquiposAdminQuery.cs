using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Queries;

public sealed record GetEquiposAdminQuery() : IRequest<IReadOnlyList<EquipoAdminResponse>>;
