namespace Umbral.Partidas.Domain.Exceptions;

public sealed class AreaBusquedaRequeridaException : Exception
{
    public AreaBusquedaRequeridaException()
        : base("El area de busqueda es requerida para un JuegoBDT.") { }
}
