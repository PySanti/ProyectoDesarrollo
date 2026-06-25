namespace Umbral.Partidas.Domain.Exceptions;

public sealed class JuegoBDTSinEtapasException : Exception
{
    public JuegoBDTSinEtapasException()
        : base("Un JuegoBDT debe tener al menos una etapa.") { }
}
