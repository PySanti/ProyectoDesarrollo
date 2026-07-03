using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
namespace Umbral.OperacionesSesion.Application.Commands;
public sealed record ValidarTesoroCommand(Guid PartidaId, Guid ParticipanteId, string ImagenBase64)
    : IRequest<ValidacionTesoroResponse>;
