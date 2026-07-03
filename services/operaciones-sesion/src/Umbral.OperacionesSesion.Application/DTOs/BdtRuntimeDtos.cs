namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record ValidacionTesoroResponse(
    Guid PartidaId, Guid EtapaId, string Resultado, bool Gano, bool CerroEtapa, int? Puntaje);

public sealed record AvanceEtapaResponse(
    Guid PartidaId, int EtapaCerradaOrden, int? EtapaActivadaOrden, bool SinMasEtapas);

public sealed record EtapaActualDto(
    Guid PartidaId, Guid JuegoId, Guid EtapaId, int Orden, string AreaBusqueda,
    int TiempoLimiteSegundos, DateTime FechaActivacion);

public sealed record ValidarTesoroRequest(string ImagenBase64);

public sealed record EnviarPistaRequest(Guid? ParticipanteDestinoId, string Texto, Guid? EquipoDestinoId = null);
public sealed record PistaEnviadaResponse(Guid PartidaId, Guid JuegoId, Guid? ParticipanteDestinoId, DateTime TimestampUtc, Guid? EquipoDestinoId = null);
