using Umbral.Partidas.Application.DTOs;
using Umbral.Partidas.Domain.Enums;

namespace Umbral.Partidas.Api.Contracts;

public sealed record CrearPartidaRequest(
    string NombrePartida,
    Modalidad Modalidad,
    ModoInicioPartida ModoInicioPartida,
    DateTime? TiempoInicio,
    int MinimosParticipacion,
    int MaximosParticipacion);

public sealed record AgregarJuegoTriviaRequest(
    int Orden,
    IReadOnlyList<PreguntaRequest> Preguntas);

public sealed record AgregarJuegoBDTRequest(
    int Orden,
    string AreaBusqueda,
    IReadOnlyList<EtapaRequest> Etapas);
