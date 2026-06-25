namespace Umbral.Partidas.Domain.Exceptions;

public sealed class EtapaBDTInvalidaException : Exception
{
    public EtapaBDTInvalidaException(string motivo)
        : base($"Etapa BDT invalida: {motivo}") { }
}
