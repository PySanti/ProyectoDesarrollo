using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Queries;

public sealed record ResolverNombresQuery(
    IReadOnlyList<Guid> ParticipanteIds,
    IReadOnlyList<Guid> EquipoIds) : IRequest<NombresResponse>;
