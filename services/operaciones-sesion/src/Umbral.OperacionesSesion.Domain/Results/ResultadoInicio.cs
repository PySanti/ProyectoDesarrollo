using Umbral.OperacionesSesion.Domain.Entities;

namespace Umbral.OperacionesSesion.Domain.Results;

public enum TipoResultadoInicio { Iniciada, Cancelada, NoCorresponde }

public sealed record ResultadoInicio(TipoResultadoInicio Tipo, JuegoResumen? JuegoActivado)
{
    public static ResultadoInicio Iniciada(JuegoResumen juegoActivado) => new(TipoResultadoInicio.Iniciada, juegoActivado);
    public static ResultadoInicio Cancelada { get; } = new(TipoResultadoInicio.Cancelada, null);
    public static ResultadoInicio NoCorresponde { get; } = new(TipoResultadoInicio.NoCorresponde, null);
}
