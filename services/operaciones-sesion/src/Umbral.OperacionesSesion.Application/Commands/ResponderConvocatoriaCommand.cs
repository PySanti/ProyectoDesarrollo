using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Commands;

public sealed record ResponderConvocatoriaCommand(Guid ConvocatoriaId, Guid UsuarioId, bool Aceptar)
    : IRequest<ConvocatoriaResponse>;
