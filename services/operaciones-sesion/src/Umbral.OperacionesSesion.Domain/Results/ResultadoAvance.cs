using Umbral.OperacionesSesion.Domain.Entities;

namespace Umbral.OperacionesSesion.Domain.Results;

public enum TipoResultadoAvance { Avanzado, Terminada }

public sealed record ResultadoAvance(TipoResultadoAvance Tipo, JuegoResumen JuegoFinalizado, JuegoResumen? JuegoActivado)
{
    public static ResultadoAvance Avanzado(JuegoResumen finalizado, JuegoResumen activado) =>
        new(TipoResultadoAvance.Avanzado, finalizado, activado);

    public static ResultadoAvance Terminada(JuegoResumen finalizado) =>
        new(TipoResultadoAvance.Terminada, finalizado, null);

    public bool Terminada() => Tipo == TipoResultadoAvance.Terminada;
}
