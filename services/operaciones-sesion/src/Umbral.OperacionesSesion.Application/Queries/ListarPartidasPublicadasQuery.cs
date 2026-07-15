using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Queries;

public sealed record ListarPartidasPublicadasQuery() : IRequest<IReadOnlyList<PartidaPublicadaDto>>;
