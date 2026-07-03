using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
namespace Umbral.OperacionesSesion.Application.Commands;
public sealed record EnviarPistaCommand(Guid PartidaId, Guid? ParticipanteDestinoId, string Texto, Guid? EquipoDestinoId = null) : IRequest<PistaEnviadaResponse>;
