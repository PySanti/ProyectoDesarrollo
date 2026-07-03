namespace Umbral.OperacionesSesion.Domain.Exceptions;
public sealed class JuegoConEtapasPendientesException : Exception
{
    public JuegoConEtapasPendientesException(Guid partidaId)
        : base($"El juego BDT de la partida {partidaId} tiene etapas pendientes.") { }
}
