namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class SesionNoEnLobbyException : Exception
{
    public SesionNoEnLobbyException(Guid partidaId)
        : base($"La sesión de la partida {partidaId} no está en Lobby.") { }
}
