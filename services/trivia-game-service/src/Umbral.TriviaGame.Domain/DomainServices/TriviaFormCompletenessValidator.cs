using System;
using System.Collections.Generic;
using System.Linq;
using Umbral.TriviaGame.Domain.Entities;

namespace Umbral.TriviaGame.Domain.DomainServices;

/// <summary>
/// Servicio de dominio que determina si un formulario de Trivia está completo
/// según las reglas de negocio definidas en RF-16 y TRIVIA-FORM-001.
///
/// Un formulario completo debe cumplir:
///   1. Contener al menos una pregunta.
///   2. Cada pregunta debe cumplir todas sus invariantes de nivel individual
///      (exactamente 4 opciones, exactamente 1 correcta).
///   3. Los textos de las opciones dentro de una misma pregunta no deben duplicarse
///      (comparación case-insensitive).
///
/// El validator es una clase estática sin estado, lo que permite usarla tanto
/// desde el aggregate root (propiedad IsComplete) como desde la capa Application
/// para enriquecer la respuesta DTO con la lista de motivos de incompletitud.
/// </summary>
public static class TriviaFormCompletenessValidator
{
    /// <summary>
    /// Determina si el formulario está completamente configurado y listo para
    /// ser asociado a una partida de Trivia (HU-17).
    /// </summary>
    /// <param name="form">Formulario de Trivia a evaluar.</param>
    /// <returns>True si el formulario cumple todas las reglas de completitud; false en caso contrario.</returns>
    public static bool IsComplete(TriviaForm form)
    {
        // Un formulario completo es aquel cuya lista de motivos de incompletitud está vacía.
        return GetIncompleteReasons(form).Count == 0;
    }

    /// <summary>
    /// Evalúa el formulario contra todas las reglas de completitud y retorna una
    /// lista legible de razones por las cuales el formulario no está completo.
    /// Si la lista está vacía, el formulario está completo.
    ///
    /// Cada razón es una cadena en español descriptiva del incumplimiento,
    /// incluyendo el texto parcial de la pregunta afectada para facilitar
    /// la corrección desde el panel del operador.
    /// </summary>
    /// <param name="form">Formulario de Trivia a evaluar.</param>
    /// <returns>Lista de cadenas con las razones de incompletitud. Vacía si el formulario está completo.</returns>
    public static IReadOnlyList<string> GetIncompleteReasons(TriviaForm form)
    {
        // Valida que el formulario no sea nulo (defensa contra llamadas incorrectas).
        if (form is null)
        {
            throw new ArgumentNullException(
                nameof(form),
                "El formulario de Trivia no puede ser nulo para evaluar su completitud.");
        }

        // Acumula todas las razones de incompletitud encontradas.
        var reasons = new List<string>();

        // -----------------------------------------------------------------------
        // REGLA 1: El formulario debe contener al menos una pregunta.
        // -----------------------------------------------------------------------
        if (form.Questions.Count == 0)
        {
            reasons.Add("El formulario no contiene ninguna pregunta. " +
                        "Debe agregar al menos una pregunta antes de usar el formulario en una partida.");
        }
        else
        {
            // -------------------------------------------------------------------
            // REGLA 2: Cada pregunta debe cumplir sus invariantes individuales.
            // -------------------------------------------------------------------
            foreach (var question in form.Questions)
            {
                // Texto parcial para identificar la pregunta en los mensajes (máx. 60 caracteres).
                var questionPreview = GetQuestionPreview(question);

                // INVARIANTE 2a: Exactamente 4 opciones por pregunta.
                if (question.Options.Count != 4)
                {
                    reasons.Add(
                        $"La pregunta \"{questionPreview}\" tiene {question.Options.Count} opciones. " +
                        "Cada pregunta debe tener exactamente 4 opciones.");
                }

                // INVARIANTE 2b: Exactamente 1 opción correcta por pregunta.
                if (!question.HasExactlyOneCorrectOption())
                {
                    // Cuenta cuántas opciones correctas hay realmente para dar un mensaje preciso.
                    var correctCount = question.Options.Count(o => o.IsCorrect);
                    reasons.Add(
                        $"La pregunta \"{questionPreview}\" tiene {correctCount} opciones marcadas como correctas. " +
                        "Cada pregunta debe tener exactamente 1 opción correcta.");
                }

                // -------------------------------------------------------------------
                // REGLA 3: Los textos de opciones dentro de una misma pregunta no deben
                //          duplicarse (comparación case-insensitive).
                // -------------------------------------------------------------------
                if (HasDuplicateOptionTexts(question))
                {
                    reasons.Add(
                        $"La pregunta \"{questionPreview}\" tiene opciones con texto duplicado. " +
                        "Todas las opciones deben tener textos distintos.");
                }
            }
        }

        // Retorna la lista completa de razones. Si está vacía, el formulario está completo.
        return reasons;
    }

    // =========================================================================
    // Métodos auxiliares privados
    // =========================================================================

    /// <summary>
    /// Obtiene un extracto del texto de la pregunta para usar en mensajes de error.
    /// Trunca a 60 caracteres para mantener los mensajes legibles en la UI.
    /// </summary>
    /// <param name="question">Entidad Question de la cual extraer el preview.</param>
    /// <returns>Texto truncado a 60 caracteres, o "sin texto" si el texto es nulo/vacío.</returns>
    private static string GetQuestionPreview(Question question)
    {
        // Obtiene el texto del value object QuestionText.
        var text = question.Text?.Value;

        // Si el texto es nulo o vacío, usa un marcador de posición.
        if (string.IsNullOrWhiteSpace(text))
        {
            return "(pregunta sin texto)";
        }

        // Trunca a 60 caracteres para mantener los mensajes manejables.
        return text.Length <= 60
            ? text
            : text.Substring(0, 60) + "...";
    }

    /// <summary>
    /// Verifica si una pregunta tiene opciones con el mismo texto, ignorando
    /// diferencias de mayúsculas/minúsculas (case-insensitive).
    /// </summary>
    /// <param name="question">Entidad Question a inspeccionar.</param>
    /// <returns>True si hay al menos dos opciones con el mismo texto; false en caso contrario.</returns>
    private static bool HasDuplicateOptionTexts(Question question)
    {
        // Obtiene los textos de todas las opciones, normalizados a minúsculas
        // para la comparación case-insensitive.
        var texts = question.Options
            .Select(o => o.Text?.Value?.ToLowerInvariant())
            .Where(t => t is not null)
            .ToList();

        // Si hay menos textos que opciones, es porque alguna opción tiene texto nulo.
        // En ese caso, se considera que hay duplicado (texto nulo no es válido como opción única).
        if (texts.Count != question.Options.Count)
        {
            return true;
        }

        // Detecta duplicados: si la cantidad de textos distintos (con Distinct())
        // es menor que la cantidad total de textos, hay al menos un duplicado.
        return texts.Distinct().Count() != texts.Count;
    }
}
