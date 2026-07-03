using System;

namespace Umbral.OperacionesSesion.Api.Realtime;

public sealed record PartidaEnLobbyPayload(Guid PartidaId);
public sealed record PartidaIniciadaPayload(Guid PartidaId);
public sealed record JuegoActivadoPayload(Guid PartidaId, Guid JuegoId, int Orden, string TipoJuego);
public sealed record PartidaCanceladaPayload(Guid PartidaId, string Motivo);
public sealed record PartidaFinalizadaPayload(Guid PartidaId);
public sealed record PreguntaActivadaPayload(Guid PartidaId, Guid JuegoId, Guid PreguntaId, int Orden, DateTime FechaLimiteUtc);
public sealed record PreguntaCerradaPayload(Guid PartidaId, Guid JuegoId, Guid PreguntaId);
public sealed record EtapaActivadaPayload(Guid PartidaId, Guid JuegoId, Guid EtapaId, int Orden, DateTime FechaLimiteUtc);
public sealed record EtapaCerradaPayload(Guid PartidaId, Guid JuegoId, Guid EtapaId);
public sealed record EtapaGanadaPayload(Guid PartidaId, Guid JuegoId, Guid EtapaId);
public sealed record UbicacionParticipantePayload(Guid PartidaId, Guid ParticipanteId, double Latitud, double Longitud, DateTime TimestampUtc);
public sealed record PistaEnviadaPayload(Guid PartidaId, Guid JuegoId, Guid? ParticipanteDestinoId, string Texto, DateTime TimestampUtc, Guid? EquipoDestinoId = null);
public sealed record ConvocatoriaCreadaPayload(Guid PartidaId, Guid EquipoId, Guid ConvocatoriaId, Guid UsuarioId);
