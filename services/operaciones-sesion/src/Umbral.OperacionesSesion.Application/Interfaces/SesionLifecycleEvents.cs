namespace Umbral.OperacionesSesion.Application.Interfaces;

public sealed record PartidaIniciadaEvent(
    Guid PartidaId, Guid SesionPartidaId, DateTime FechaInicio, Guid PrimerJuegoId, int PrimerJuegoOrden);

public sealed record JuegoActivadoEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, int Orden, string TipoJuego);

public sealed record PartidaCanceladaEvent(
    Guid PartidaId, Guid SesionPartidaId, string Motivo, DateTime FechaCancelacion);

public sealed record PartidaFinalizadaEvent(
    Guid PartidaId, Guid SesionPartidaId, DateTime FechaFin);
