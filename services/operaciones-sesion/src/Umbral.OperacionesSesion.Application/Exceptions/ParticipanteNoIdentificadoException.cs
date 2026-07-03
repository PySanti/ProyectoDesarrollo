namespace Umbral.OperacionesSesion.Application.Exceptions;

public sealed class ParticipanteNoIdentificadoException : Exception
{
    public ParticipanteNoIdentificadoException() : base("No se pudo identificar al participante desde el token.") { }
}
