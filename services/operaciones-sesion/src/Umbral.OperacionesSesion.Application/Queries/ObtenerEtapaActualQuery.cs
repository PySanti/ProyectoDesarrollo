using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
namespace Umbral.OperacionesSesion.Application.Queries;
public sealed record ObtenerEtapaActualQuery(Guid PartidaId) : IRequest<EtapaActualDto>;
