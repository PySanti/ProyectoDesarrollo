using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
namespace Umbral.OperacionesSesion.Application.Commands;
public sealed record ResponderPreguntaCommand(Guid PartidaId, Guid ParticipanteId, Guid OpcionId) : IRequest<RespuestaTriviaResponse>;
