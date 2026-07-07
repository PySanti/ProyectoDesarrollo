using MediatR;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Application.Commands;

public sealed record ProyectarJuegoActivadoCommand(
    Guid EventId, DateTime OccurredAt, Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, int Orden, TipoJuego TipoJuego) : IRequest;
