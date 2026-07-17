using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Queries;

public sealed record ResolverNombresPartidaQuery(IReadOnlyList<Guid> PartidaIds)
    : IRequest<ResolverNombresPartidaResponse>;
