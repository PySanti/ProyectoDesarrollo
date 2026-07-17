namespace Umbral.IdentityService.Api.Contracts;

public sealed record ResolverNombresRequest(
    IReadOnlyList<Guid>? ParticipanteIds,
    IReadOnlyList<Guid>? EquipoIds);
