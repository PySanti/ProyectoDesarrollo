using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Queries;

public sealed record ObtenerMisConvocatoriasPendientesQuery(Guid UsuarioId)
    : IRequest<IReadOnlyList<ConvocatoriaPendienteDto>>;
