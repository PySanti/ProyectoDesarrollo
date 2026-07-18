namespace Umbral.OperacionesSesion.Domain.Exceptions;

// Solo la lanza el inicio MANUAL: la partida sigue en Lobby y el operador puede aceptar las
// solicitudes pendientes y reintentar. El inicio por tiempo no la usa — ahí, no alcanzar los
// mínimos cancela la partida (domain-model-summary.md §Partida).
public sealed class MinimosNoAlcanzadosException : Exception
{
    public MinimosNoAlcanzadosException(Guid partidaId, int confirmadas, int minimos)
        : base($"La partida {partidaId} tiene {confirmadas} participación(es) confirmada(s) y "
            + $"requiere {minimos} para iniciar.") { }
}
