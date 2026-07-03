using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
namespace Umbral.OperacionesSesion.Application.Queries;
public sealed record ObtenerPreguntaActualQuery(Guid PartidaId) : IRequest<PreguntaActualDto>;
