namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record ConfiguracionPartidaDto(
    string Nombre,
    string Modalidad,
    string ModoInicioPartida,
    DateTime? TiempoInicio,
    int MinimosParticipacion,
    int MaximosParticipacion,
    IReadOnlyList<JuegoResumenDto> Juegos);

public sealed record JuegoResumenDto(Guid JuegoId, int Orden, string TipoJuego, TriviaConfigDto? Trivia = null, BdtConfigDto? Bdt = null);

public sealed record BdtConfigDto(string AreaBusqueda, IReadOnlyList<EtapaConfigDto> Etapas);

public sealed record EtapaConfigDto(
    Guid EtapaBDTId, int Orden, string CodigoQREsperado, int PuntajeAsignado, int TiempoLimiteSegundos);

public sealed record TriviaConfigDto(IReadOnlyList<PreguntaConfigDto> Preguntas);

public sealed record PreguntaConfigDto(
    Guid PreguntaId, string Texto, int PuntajeAsignado, int TiempoLimiteSegundos,
    IReadOnlyList<OpcionConfigDto> Opciones);

public sealed record OpcionConfigDto(Guid OpcionId, string Texto, bool EsCorrecta);
