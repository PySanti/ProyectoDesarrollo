using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
namespace Umbral.OperacionesSesion.Application.Queries;
public sealed record ObtenerEnviosTesoroQuery(Guid PartidaId) : IRequest<EnviosTesoroDto>;
