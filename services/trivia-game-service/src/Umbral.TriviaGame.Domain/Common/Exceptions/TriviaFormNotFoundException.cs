namespace Umbral.TriviaGame.Domain.Common.Exceptions;

/// <summary>
/// Excepción que se lanza cuando se intenta acceder a un formulario de Trivia
/// que no existe en el repositorio. Se mapea a HTTP 404 en la capa de API.
/// </summary>
public sealed class TriviaFormNotFoundException : DomainValidationException
{
    /// <summary>
    /// Identificador del formulario que no fue encontrado.
    /// </summary>
    public Guid FormId { get; }

    /// <summary>
    /// Inicializa la excepción con el identificador del formulario no encontrado.
    /// </summary>
    /// <param name="formId">Identificador GUID del formulario solicitado.</param>
    public TriviaFormNotFoundException(Guid formId)
        : base($"No se encontró un formulario de Trivia con el identificador '{formId}'.")
    {
        FormId = formId;
    }
}
