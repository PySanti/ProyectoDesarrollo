using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Domain.Results;

public sealed record ResultadoRegistroTesoro(
    ResultadoValidacionQR Resultado,
    bool CerroEtapa,
    bool Gano,
    int? Puntaje,
    Guid JuegoId,
    Guid EtapaId,
    Guid ParticipanteId,
    Guid? GanadorParticipanteId,
    long? TiempoResolucionMs,
    string? QrDecodificado,
    DateTime Instante,
    Guid? EquipoId = null,
    Guid? GanadorEquipoId = null,
    // Set cuando ganar/cerrar cierra la ÚLTIMA etapa del juego: el juego se finaliza
    // (avanza al siguiente o termina la partida), igual que el camino por timeout.
    ResultadoAvance? JuegoFinalizado = null);
