namespace Umbral.TriviaGame.Domain.Common.Exceptions;

public sealed class FormularioIncompletoException : DomainValidationException
{
    public Guid FormularioId { get; }

    public FormularioIncompletoException(Guid formularioId)
        : base($"El formulario de Trivia con identificador '{formularioId}' no está completo. " +
               "No se puede crear una partida con un formulario incompleto.")
    {
        FormularioId = formularioId;
    }
}
