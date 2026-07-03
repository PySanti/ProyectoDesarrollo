namespace Umbral.OperacionesSesion.Domain.Exceptions;
public sealed class JuegoActivoNoEsBDTException : Exception
{
    public JuegoActivoNoEsBDTException(Guid partidaId)
        : base($"El juego activo de la partida {partidaId} no es de Búsqueda del Tesoro.") { }
}
