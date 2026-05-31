using System;

namespace Umbral.TriviaGame.Domain.Common.Exceptions;

/// <summary>
/// Excepción lanzada cuando una pregunta no tiene exactamente 4 opciones de respuesta.
/// Regla HU-15-FORM-001: cada pregunta debe tener exactamente 4 opciones.
/// </summary>
public sealed class InvalidQuestionOptionsCountException : DomainValidationException
{
    /// <summary>
    /// Constructor que recibe la cantidad actual de opciones para incluirla en el mensaje.
    /// </summary>
    /// <param name="actualCount">Número de opciones que se intentaron asignar a la pregunta.</param>
    public InvalidQuestionOptionsCountException(int actualCount)
        : base($"Cada pregunta debe tener exactamente 4 opciones. Se recibieron {actualCount}.")
    {
    }
}
