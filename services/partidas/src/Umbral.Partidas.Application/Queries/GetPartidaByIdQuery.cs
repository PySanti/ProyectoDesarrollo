using MediatR;
using Umbral.Partidas.Application.DTOs;

namespace Umbral.Partidas.Application.Queries;

public sealed record GetPartidaByIdQuery(Guid PartidaId) : IRequest<PartidaDetailDto>;
