using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Commands;

public sealed record InscribirParticipanteCommand(Guid PartidaId, Guid ParticipanteId) : IRequest<InscripcionResponse>;
