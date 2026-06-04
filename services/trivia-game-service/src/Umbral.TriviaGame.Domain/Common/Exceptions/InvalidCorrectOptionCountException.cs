using System;

namespace Umbral.TriviaGame.Domain.Common.Exceptions;

/// <summary>
/// Excepción lanzada cuando una pregunta no tiene exactamente una opción marcada como correcta.
/// Regla HU-15-FORM-002: cada pregunta debe tener exactamente 1 opción correcta.
/// </summary>
public sealed class InvalidCorrectOptionCountException : DomainValidationException
{
    /// <summary>
    /// Constructor que recibe la cantidad de opciones correctas detectadas para incluirla en el mensaje.
    /// </summary>
    /// <param name="correctCount">Número de opciones marcadas como correctas (0 o más de 1).</param>
    public InvalidCorrectOptionCountException(int correctCount)
        : base($"Cada pregunta debe tener exactamente 1 opción correcta. Se encontraron {correctCount}.")
    {
    }
}
