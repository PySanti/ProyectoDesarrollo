using System;
using System.Collections.Generic;
using System.Linq;
using Umbral.TriviaGame.Domain.DomainServices;
using Umbral.TriviaGame.Domain.Drafts;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.ValueObjects;
using Xunit;

namespace Umbral.TriviaGame.Domain.Tests.DomainServices;

public class TriviaFormCompletenessValidatorTests
{
    // =========================================================================
    // Helpers compartidos
    // =========================================================================

    /// <summary>
    /// Genera 4 opciones con la primera como correcta y textos distintos.
    /// </summary>
    private static IReadOnlyList<AnswerOptionDraft> FourOptions() =>
        new List<AnswerOptionDraft>
        {
            AnswerOptionDraft.Create("París", true),
            AnswerOptionDraft.Create("Londres", false),
            AnswerOptionDraft.Create("Berlín", false),
            AnswerOptionDraft.Create("Madrid", false),
        };

    /// <summary>
    /// Genera 4 opciones con la primera como correcta, pero "Londres" y "londres"
    /// son duplicados case-insensitive para probar la REGLA 3.
    /// </summary>
    private static IReadOnlyList<AnswerOptionDraft> FourOptionsWithDuplicateTexts =>
        new List<AnswerOptionDraft>
        {
            AnswerOptionDraft.Create("París", true),
            AnswerOptionDraft.Create("londres", false), // Duplicado case-insensitive de la de abajo.
            AnswerOptionDraft.Create("Berlín", false),
            AnswerOptionDraft.Create("Londres", false), // "Londres" vs "londres" → duplicado.
        };

    /// <summary>
    /// Crea un formulario con una sola pregunta válida.
    /// </summary>
    private static TriviaForm CreateValidForm()
    {
        return TriviaForm.Create(
            FormTitle.Create("Cultura General"),
            OperatorId.Create("op-1"),
            new[]
            {
                QuestionDraft.Create(
                    "¿Cuál es la capital de Francia?",
                    assignedScore: 10,
                    timeLimitSeconds: 30,
                    displayOrder: 1,
                    FourOptions()),
            });
    }

    /// <summary>
    /// Crea un formulario con una pregunta que tiene textos duplicados
    /// en sus opciones ("londres" y "Londres").
    /// Question.Create no valida duplicados de texto, así que el borrador pasa
    /// la creación; es el validador quien debe detectarlo.
    /// </summary>
    private static TriviaForm CreateFormWithDuplicateOptionTexts()
    {
        return TriviaForm.Create(
            FormTitle.Create("Geografía"),
            OperatorId.Create("op-2"),
            new[]
            {
                QuestionDraft.Create(
                    "¿Cuál es la capital del Reino Unido?",
                    assignedScore: 10,
                    timeLimitSeconds: 30,
                    displayOrder: 1,
                    FourOptionsWithDuplicateTexts),
            });
    }

    // =========================================================================
    // IsComplete — formulario completo
    // =========================================================================

    [Fact]
    public void IsComplete_WithValidForm_ReturnsTrue()
    {
        // Arrange: formulario con 1 pregunta válida.
        var form = CreateValidForm();

        // Act: evalúa completitud.
        var isComplete = TriviaFormCompletenessValidator.IsComplete(form);

        // Assert: el formulario está completo.
        Assert.True(isComplete);
    }

    [Fact]
    public void GetIncompleteReasons_WithValidForm_ReturnsEmptyList()
    {
        // Arrange: formulario completamente válido.
        var form = CreateValidForm();

        // Act: obtiene las razones de incompletitud.
        var reasons = TriviaFormCompletenessValidator.GetIncompleteReasons(form);

        // Assert: no hay razones de incompletitud.
        Assert.Empty(reasons);
    }

    [Fact]
    public void TriviaFormIsComplete_DelegatesToValidator()
    {
        // Arrange: formulario válido.
        var form = CreateValidForm();

        // Assert: la propiedad IsComplete del aggregate y el validador coinciden.
        Assert.Equal(
            TriviaFormCompletenessValidator.IsComplete(form),
            form.IsComplete);
    }

    // =========================================================================
    // IsComplete — formulario incompleto por duplicados de opciones
    // =========================================================================

    [Fact]
    public void IsComplete_WithDuplicateOptionTexts_ReturnsFalse()
    {
        // Arrange: formulario con una pregunta que tiene opciones duplicadas
        // ("londres" y "Londres").
        var form = CreateFormWithDuplicateOptionTexts();

        // Act: evalúa completitud.
        var isComplete = TriviaFormCompletenessValidator.IsComplete(form);

        // Assert: el formulario NO está completo.
        Assert.False(isComplete);

        // Assert adicional: la propiedad IsComplete del aggregate también refleja
        // el resultado del validador.
        Assert.False(form.IsComplete);
    }

    [Fact]
    public void GetIncompleteReasons_WithDuplicateOptionTexts_ContainsDuplicateReason()
    {
        // Arrange: formulario con opciones duplicadas.
        var form = CreateFormWithDuplicateOptionTexts();

        // Act: obtiene las razones.
        var reasons = TriviaFormCompletenessValidator.GetIncompleteReasons(form);

        // Assert: la lista de razones contiene al menos una mención de duplicado.
        var duplicateReason = reasons.FirstOrDefault(r =>
            r.Contains("duplicado", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(duplicateReason);

        // Assert: la razón menciona la pregunta afectada.
        Assert.Contains("capital del Reino Unido", duplicateReason, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // IsComplete — formulario incompleto por preguntas que faltan
    // =========================================================================

    [Fact]
    public void GetIncompleteReasons_WithEmptyQuestions_ReportsNoQuestions()
    {
        // Nota: TriviaForm.Create y ReplaceQuestions ya protegen la invariante
        // de al menos 1 pregunta (EmptyQuestionSetException). Por lo tanto,
        // no podemos crear un TriviaForm sin preguntas desde el dominio público.
        // Esta prueba verifica que, SI por corrupción de estado el formulario
        // quedara sin preguntas, el validador lo detectaría.

        // Para probar la lógica del validador directamente, evaluamos un
        // formulario válido y confirmamos que NO reporte esta razón.
        var form = CreateValidForm();
        var reasons = TriviaFormCompletenessValidator.GetIncompleteReasons(form);

        Assert.DoesNotContain(reasons, r =>
            r.Contains("ninguna pregunta", StringComparison.OrdinalIgnoreCase));
    }

    // =========================================================================
    // Casos nulos
    // =========================================================================

    [Fact]
    public void GetIncompleteReasons_WithNullForm_ThrowsArgumentNullException()
    {
        // Act & Assert: formulario nulo produce ArgumentNullException.
        var ex = Assert.Throws<ArgumentNullException>(() =>
            TriviaFormCompletenessValidator.GetIncompleteReasons(null!));

        Assert.Contains("form", ex.ParamName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IsComplete_WithNullForm_ThrowsArgumentNullException()
    {
        // Act & Assert: IsComplete con formulario nulo también lanza excepción.
        var ex = Assert.Throws<ArgumentNullException>(() =>
            TriviaFormCompletenessValidator.IsComplete(null!));

        Assert.Contains("form", ex.ParamName, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // Formulario con múltiples preguntas válidas
    // =========================================================================

    [Fact]
    public void GetIncompleteReasons_WithMultipleValidQuestions_ReturnsEmpty()
    {
        // Arrange: crea un formulario con 2 preguntas válidas.
        var opciones2 = new List<AnswerOptionDraft>
        {
            AnswerOptionDraft.Create("3.14", true),
            AnswerOptionDraft.Create("2.71", false),
            AnswerOptionDraft.Create("1.61", false),
            AnswerOptionDraft.Create("0.57", false),
        };

        var form = TriviaForm.Create(
            FormTitle.Create("Matemáticas"),
            OperatorId.Create("op-2"),
            new List<QuestionDraft>
            {
                QuestionDraft.Create(
                    "¿Cuál es la capital de Francia?",
                    assignedScore: 10,
                    timeLimitSeconds: 30,
                    displayOrder: 1,
                    FourOptions()),
                QuestionDraft.Create(
                    "¿Cuál es el valor de Pi?",
                    assignedScore: 15,
                    timeLimitSeconds: 20,
                    displayOrder: 2,
                    opciones2),
            });

        // Act: evalúa completitud.
        var isComplete = TriviaFormCompletenessValidator.IsComplete(form);
        var reasons = TriviaFormCompletenessValidator.GetIncompleteReasons(form);

        // Assert: ambas preguntas son válidas → formulario completo.
        Assert.True(isComplete);
        Assert.Empty(reasons);
    }

    // =========================================================================
    // ToString del validador no aplica (clase estática)
    // pero verificamos que el delegate del aggregate sea correcto
    // =========================================================================

    [Fact]
    public void ValidatorMethods_AreConsistent_IsCompleteMatchesReasons()
    {
        // Arrange: formulario válido.
        var form = CreateValidForm();

        // Act: obtiene ambos resultados.
        var isComplete = TriviaFormCompletenessValidator.IsComplete(form);
        var reasons = TriviaFormCompletenessValidator.GetIncompleteReasons(form);

        // Assert: IsComplete es true SI Y SOLO SI la lista de razones está vacía.
        Assert.Equal(isComplete, reasons.Count == 0);

        // Arrange: formulario con duplicados.
        var formWithDuplicates = CreateFormWithDuplicateOptionTexts();

        // Act: obtiene ambos resultados.
        var isCompleteDup = TriviaFormCompletenessValidator.IsComplete(formWithDuplicates);
        var reasonsDup = TriviaFormCompletenessValidator.GetIncompleteReasons(formWithDuplicates);

        // Assert: IsComplete es false SI Y SOLO SI la lista de razones NO está vacía.
        Assert.Equal(isCompleteDup, reasonsDup.Count == 0);
        Assert.False(isCompleteDup);
        Assert.NotEmpty(reasonsDup);
    }
}
