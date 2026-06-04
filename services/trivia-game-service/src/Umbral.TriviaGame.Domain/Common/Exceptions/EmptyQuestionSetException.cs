namespace Umbral.TriviaGame.Domain.Common.Exceptions;

/// <summary>
/// Excepción que se lanza cuando se intenta crear o actualizar un formulario de Trivia
/// con un conjunto vacío de preguntas. El dominio exige al menos 1 pregunta por formulario
/// para que pueda ser utilizado en una partida (RF-16 / TRIVIA-FORM-001).
/// </summary>
public sealed class EmptyQuestionSetException : DomainValidationException
{
    /// <summary>
    /// Inicializa la excepción con un mensaje descriptivo en español.
    /// </summary>
    public EmptyQuestionSetException()
        : base("El formulario debe contener al menos una pregunta.")
    {
    }
}
