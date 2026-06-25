namespace Umbral.Partidas.Application.DTOs;

public sealed record PartidaDetailDto(
    Guid PartidaId,
    string NombrePartida,
    string Modalidad,
    string ModoInicioPartida,
    DateTime? TiempoInicio,
    int MinimosParticipacion,
    int MaximosParticipacion,
    string? Estado,
    IReadOnlyList<JuegoDto> Juegos);

public sealed record JuegoDto(
    Guid JuegoId,
    int Orden,
    string TipoJuego,
    string Estado,
    TriviaContenidoDto? Trivia,
    BDTContenidoDto? BDT);

public sealed record TriviaContenidoDto(IReadOnlyList<PreguntaDto> Preguntas);

public sealed record PreguntaDto(
    Guid PreguntaId,
    string Texto,
    int PuntajeAsignado,
    int TiempoLimiteSegundos,
    IReadOnlyList<OpcionDto> Opciones);

public sealed record OpcionDto(Guid OpcionId, string Texto, bool EsCorrecta);

public sealed record BDTContenidoDto(string AreaBusqueda, IReadOnlyList<EtapaDto> Etapas);

public sealed record EtapaDto(
    Guid EtapaBDTId,
    int Orden,
    string CodigoQREsperado,
    int PuntajeAsignado,
    int TiempoLimiteSegundos);
