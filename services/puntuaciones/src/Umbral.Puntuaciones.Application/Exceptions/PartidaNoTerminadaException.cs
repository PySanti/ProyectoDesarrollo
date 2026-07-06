using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Application.Exceptions;

// El consolidado se calcula al finalizar (RF-45/HU-50): en cualquier otro estado es 409.
public sealed class PartidaNoTerminadaException : Exception
{
    public PartidaNoTerminadaException(Guid partidaId, EstadoPartidaProyectada estado)
        : base($"La partida {partidaId} no está terminada (estado {estado}); el ranking consolidado se calcula al finalizar.")
    {
    }
}
