using MediatR;
using Umbral.Partidas.Application.DTOs;
using Umbral.Partidas.Domain.Enums;

namespace Umbral.Partidas.Application.Commands;

public sealed record CrearPartidaCommand(
    string NombrePartida,
    Modalidad Modalidad,
    ModoInicioPartida ModoInicioPartida,
    DateTime? TiempoInicio,
    int MinimosParticipacion,
    int MaximosParticipacion) : IRequest<CrearPartidaResponse>;
