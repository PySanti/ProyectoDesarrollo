namespace Umbral.TriviaGame.Domain.Common.Exceptions;

public sealed class ModalidadInvalidaException : DomainValidationException
{
    public string Modalidad { get; }
    public string Detalle { get; }

    public ModalidadInvalidaException(string modalidad, string detalle)
        : base($"Configuración inválida para la modalidad '{modalidad}': {detalle}")
    {
        Modalidad = modalidad;
        Detalle = detalle;
    }
}
