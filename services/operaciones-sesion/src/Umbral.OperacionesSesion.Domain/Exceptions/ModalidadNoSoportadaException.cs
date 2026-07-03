namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class ModalidadNoSoportadaException : Exception
{
    public ModalidadNoSoportadaException(Guid partidaId)
        : base($"La modalidad de la partida {partidaId} no está soportada en esta operación (Equipo llega en SP-3a-E).") { }
}
