using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Queries;

public sealed record ListarEquiposQuery() : IRequest<IReadOnlyList<EquipoAdminItemResponse>>;
