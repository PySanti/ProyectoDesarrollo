namespace Umbral.OperacionesSesion.Domain.Exceptions;
public sealed class PreguntaFueraDeTiempoException : Exception
{
    public PreguntaFueraDeTiempoException(Guid preguntaId)
        : base($"La pregunta {preguntaId} está fuera de su ventana de tiempo.") { }
}
