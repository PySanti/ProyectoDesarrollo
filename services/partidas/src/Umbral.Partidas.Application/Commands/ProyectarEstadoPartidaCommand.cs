using MediatR;
using Umbral.Partidas.Domain.Enums;

namespace Umbral.Partidas.Application.Commands;

// Proyecta en Partidas el estado de runtime que Operaciones de Sesión reporta por RabbitMQ
// (PartidaPublicadaEnLobby/Iniciada/Cancelada/Finalizada). No es una acción de usuario: lo
// despacha el consumidor de eventos.
public sealed record ProyectarEstadoPartidaCommand(Guid PartidaId, EstadoPartida Estado) : IRequest;
