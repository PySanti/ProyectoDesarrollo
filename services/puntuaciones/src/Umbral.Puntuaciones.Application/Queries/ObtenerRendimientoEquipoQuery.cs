using MediatR;
using Umbral.Puntuaciones.Application.DTOs;

namespace Umbral.Puntuaciones.Application.Queries;

public sealed record ObtenerRendimientoEquipoQuery(Guid EquipoId) : IRequest<RendimientoEquipoResponse>;
