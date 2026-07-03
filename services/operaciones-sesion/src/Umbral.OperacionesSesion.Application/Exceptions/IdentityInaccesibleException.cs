namespace Umbral.OperacionesSesion.Application.Exceptions;

public sealed class IdentityInaccesibleException : Exception
{
    public IdentityInaccesibleException() : base("El servicio Identity no está accesible.") { }
    public IdentityInaccesibleException(Exception inner) : base("El servicio Identity no está accesible.", inner) { }
}
