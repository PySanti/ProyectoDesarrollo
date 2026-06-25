using MediatR;
using Umbral.Partidas.Application.DTOs;

namespace Umbral.Partidas.Application.Queries;

public sealed record ListPartidasQuery() : IRequest<IReadOnlyList<PartidaSummaryDto>>;
