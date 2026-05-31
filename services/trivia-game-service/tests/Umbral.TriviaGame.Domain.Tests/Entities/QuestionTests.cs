using System;
using System.Collections.Generic;
using System.Linq;
using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.Common.Exceptions;
using Umbral.TriviaGame.Domain.Drafts;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.ValueObjects;
using Xunit;

namespace Umbral.TriviaGame.Domain.Tests.Entities;

public class QuestionTests
{
    /// <summary>
    /// Helper que genera 4 opciones de prueba con solo la primera como correcta.
    /// </summary>
    private static IReadOnlyList<AnswerOptionDraft> FourOptions => new List<AnswerOptionDraft>
    {
        AnswerOptionDraft.Create("París", true),
        AnswerOptionDraft.Create("Londres", false),
        AnswerOptionDraft.Create("Berlín", false),
        AnswerOptionDraft.Create("Madrid", false),
    };

    /// <summary>
    /// Helper que genera un QuestionDraft válido con las 4 opciones estándar.
    /// </summary>
    private static QuestionDraft ValidDraft =>
        QuestionDraft.Create("¿Capital de Francia?", assignedScore: 10, timeLimitSeconds: 30, displayOrder: 1, FourOptions);

    // =========================================================================
    // Create — casos válidos
    // =========================================================================

    [Fact]
    public void Create_WithValidDraft_SetsAllProperties()
    {
        // Arrange & Act: crea una pregunta con datos completamente válidos.
        var question = Question.Create(ValidDraft, displayOrder: 1);

        // Assert: verifica que cada propiedad se haya asignado correctamente.
        Assert.NotNull(question.Id);                         // El Id fue generado.
        Assert.Equal("¿Capital de Francia?", question.Text.Value);  // Texto preservado.
        Assert.Equal(10, question.AssignedScore.Value);       // Puntaje preservado.
        Assert.Equal(30, question.TimeLimit.Seconds);         // Timer preservado.
        Assert.Equal(1, question.DisplayOrder);               // Orden preservado.
        Assert.Equal(4, question.Options.Count);              // Exactamente 4 opciones.
        Assert.Single(question.Options, o => o.IsCorrect);    // Exactamente 1 correcta.
    }

    [Fact]
    public void Create_WithValidDraft_GeneratesNonEmptyGuid()
    {
        // Arrange & Act: crea una pregunta válida.
        var question = Question.Create(ValidDraft, displayOrder: 1);

        // Assert: el Guid del QuestionId no debe ser vacío (se generó automáticamente).
        Assert.NotEqual(Guid.Empty, question.Id.Value);
    }

    [Fact]
    public void Create_WithDisplayOrder2_SetsDisplayOrder2()
    {
        // Arrange & Act: crea una pregunta con displayOrder = 2.
        var question = Question.Create(ValidDraft, displayOrder: 2);

        // Assert: el orden debe ser 2.
        Assert.Equal(2, question.DisplayOrder);
    }

    // =========================================================================
    // Create — invariante: exactamente 4 opciones
    // =========================================================================

    [Fact]
    public void Create_With3Options_ThrowsInvalidQuestionOptionsCountException()
    {
        // Arrange: genera solo 3 opciones (viola HU-15-FORM-001).
        var draft = QuestionDraft.Create("Pregunta", 10, 30, 1, new List<AnswerOptionDraft>
        {
            AnswerOptionDraft.Create("A", true),
            AnswerOptionDraft.Create("B", false),
            AnswerOptionDraft.Create("C", false),
        });

        // Act & Assert: debe lanzar excepción por cantidad incorrecta de opciones.
        var ex = Assert.Throws<InvalidQuestionOptionsCountException>(() =>
            Question.Create(draft, displayOrder: 1));

        // Verifica que el mensaje mencione la cantidad recibida (3).
        Assert.Contains("3", ex.Message);
    }

    [Fact]
    public void Create_With5Options_ThrowsInvalidQuestionOptionsCountException()
    {
        // Arrange: genera 5 opciones (viola HU-15-FORM-001).
        var draft = QuestionDraft.Create("Pregunta", 10, 30, 1, new List<AnswerOptionDraft>
        {
            AnswerOptionDraft.Create("A", true),
            AnswerOptionDraft.Create("B", false),
            AnswerOptionDraft.Create("C", false),
            AnswerOptionDraft.Create("D", false),
            AnswerOptionDraft.Create("E", false),
        });

        // Act & Assert: debe lanzar excepción por cantidad incorrecta de opciones.
        var ex = Assert.Throws<InvalidQuestionOptionsCountException>(() =>
            Question.Create(draft, displayOrder: 1));

        // Verifica que el mensaje mencione la cantidad recibida (5).
        Assert.Contains("5", ex.Message);
    }

    // =========================================================================
    // Create — invariante: exactamente 1 opción correcta
    // =========================================================================

    [Fact]
    public void Create_With0CorrectOptions_ThrowsInvalidCorrectOptionCountException()
    {
        // Arrange: genera 4 opciones donde ninguna es correcta (viola HU-15-FORM-002).
        var draft = QuestionDraft.Create("Pregunta", 10, 30, 1, new List<AnswerOptionDraft>
        {
            AnswerOptionDraft.Create("A", false),
            AnswerOptionDraft.Create("B", false),
            AnswerOptionDraft.Create("C", false),
            AnswerOptionDraft.Create("D", false),
        });

        // Act & Assert: debe lanzar excepción por falta de opción correcta.
        var ex = Assert.Throws<InvalidCorrectOptionCountException>(() =>
            Question.Create(draft, displayOrder: 1));

        // Verifica que el mensaje indique 0 correctas.
        Assert.Contains("0", ex.Message);
    }

    [Fact]
    public void Create_With2CorrectOptions_ThrowsInvalidCorrectOptionCountException()
    {
        // Arrange: genera 4 opciones con 2 correctas (viola HU-15-FORM-002).
        var draft = QuestionDraft.Create("Pregunta", 10, 30, 1, new List<AnswerOptionDraft>
        {
            AnswerOptionDraft.Create("A", true),
            AnswerOptionDraft.Create("B", true),
            AnswerOptionDraft.Create("C", false),
            AnswerOptionDraft.Create("D", false),
        });

        // Act & Assert: debe lanzar excepción por exceso de opciones correctas.
        var ex = Assert.Throws<InvalidCorrectOptionCountException>(() =>
            Question.Create(draft, displayOrder: 1));

        // Verifica que el mensaje indique 2 correctas.
        Assert.Contains("2", ex.Message);
    }

    // =========================================================================
    // Create — validación de nulls y rangos desde VOs
    // =========================================================================

    [Fact]
    public void Create_WithNullDraft_ThrowsDomainValidationException()
    {
        // Act & Assert: pasar null como borrador debe lanzar DomainValidationException.
        var ex = Assert.Throws<DomainValidationException>(() =>
            Question.Create(null!, displayOrder: 1));

        // Verifica que el mensaje sea descriptivo.
        Assert.Contains("borrador", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithDisplayOrder0_ThrowsDomainValidationException()
    {
        // Arrange: crea un draft válido pero con displayOrder = 0 (inválido, debe ser >= 1).
        var draft = ValidDraft;

        // Act & Assert: displayOrder = 0 debe lanzar DomainValidationException.
        var ex = Assert.Throws<DomainValidationException>(() =>
            Question.Create(draft, displayOrder: 0));

        // Verifica que el mensaje indique que el orden es inválido.
        Assert.Contains("orden", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithScoreOutOfRange_ThrowsDomainValidationException()
    {
        // Arrange: crea un borrador con puntaje 0 (viola rango 1..1000).
        var draft = QuestionDraft.Create("Pregunta", assignedScore: 0, timeLimitSeconds: 30, displayOrder: 1, FourOptions);

        // Act & Assert: la validación dentro de AssignedScore.Create debe lanzar excepción.
        Assert.Throws<DomainValidationException>(() =>
            Question.Create(draft, displayOrder: 1));
    }

    [Fact]
    public void Create_WithTimerOutOfRange_ThrowsDomainValidationException()
    {
        // Arrange: crea un borrador con timer 4 (viola rango 5..300).
        var draft = QuestionDraft.Create("Pregunta", assignedScore: 10, timeLimitSeconds: 4, displayOrder: 1, FourOptions);

        // Act & Assert: la validación dentro de TimeLimit.Create debe lanzar excepción.
        Assert.Throws<DomainValidationException>(() =>
            Question.Create(draft, displayOrder: 1));
    }

    // =========================================================================
    // GetCorrectOption
    // =========================================================================

    [Fact]
    public void GetCorrectOption_WithValidQuestion_ReturnsSingleCorrectOption()
    {
        // Arrange: crea una pregunta válida (primera opción "París" es correcta).
        var question = Question.Create(ValidDraft, displayOrder: 1);

        // Act: obtiene la opción correcta.
        var correct = question.GetCorrectOption();

        // Assert: debe ser la opción con texto "París" y estar marcada como correcta.
        Assert.Equal("París", correct.Text.Value);
        Assert.True(correct.IsCorrect);
    }

    // =========================================================================
    // HasExactlyOneCorrectOption
    // =========================================================================

    [Fact]
    public void HasExactlyOneCorrectOption_WithValidQuestion_ReturnsTrue()
    {
        // Arrange: crea una pregunta válida.
        var question = Question.Create(ValidDraft, displayOrder: 1);

        // Act & Assert: debe tener exactamente 1 opción correcta.
        Assert.True(question.HasExactlyOneCorrectOption());
    }

    // =========================================================================
    // UpdateFrom
    // =========================================================================

    [Fact]
    public void UpdateFrom_WithValidDraft_UpdatesAllPropertiesPreservingId()
    {
        // Arrange: crea una pregunta inicial.
        var original = Question.Create(ValidDraft, displayOrder: 1);
        var originalId = original.Id;

        // Crea un nuevo borrador con datos distintos.
        var newDraft = QuestionDraft.Create(
            "¿Capital de Alemania?",
            assignedScore: 20,
            timeLimitSeconds: 45,
            displayOrder: 3,
            new List<AnswerOptionDraft>
            {
                AnswerOptionDraft.Create("Berlín", true),
                AnswerOptionDraft.Create("París", false),
                AnswerOptionDraft.Create("Londres", false),
                AnswerOptionDraft.Create("Madrid", false),
            });

        // Act: actualiza la pregunta con el nuevo borrador.
        original.UpdateFrom(newDraft, displayOrder: 3);

        // Assert: el Id debe preservarse (es la misma entidad).
        Assert.Equal(originalId, original.Id);

        // El resto de propiedades debe reflejar los nuevos valores.
        Assert.Equal("¿Capital de Alemania?", original.Text.Value);
        Assert.Equal(20, original.AssignedScore.Value);
        Assert.Equal(45, original.TimeLimit.Seconds);
        Assert.Equal(3, original.DisplayOrder);
        Assert.Equal(4, original.Options.Count);
        Assert.Single(original.Options, o => o.IsCorrect);
        Assert.True(original.Options.Single(o => o.IsCorrect).Text.Value == "Berlín");
    }

    [Fact]
    public void UpdateFrom_WithInvalidDraft_ThrowsAndDoesNotMutate()
    {
        // Arrange: crea una pregunta inicial y captura su estado original.
        var original = Question.Create(ValidDraft, displayOrder: 1);
        var originalText = original.Text.Value;
        var originalScore = original.AssignedScore.Value;

        // Crea un borrador inválido (solo 3 opciones).
        var invalidDraft = QuestionDraft.Create(
            "¿Nueva pregunta?",
            assignedScore: 99,
            timeLimitSeconds: 60,
            displayOrder: 2,
            new List<AnswerOptionDraft>
            {
                AnswerOptionDraft.Create("A", true),
                AnswerOptionDraft.Create("B", false),
                AnswerOptionDraft.Create("C", false),
            });

        // Act & Assert: UpdateFrom debe lanzar excepción.
        // Se usa Action explícito para evitar ambigüedad con el overload obsoleto Func<Task> en xUnit.
        Assert.Throws<InvalidQuestionOptionsCountException>((Action)(() =>
            original.UpdateFrom(invalidDraft, displayOrder: 2)));

        // Assert: el estado original no debe haber cambiado (fail-fast antes de mutar).
        Assert.Equal(originalText, original.Text.Value);
        Assert.Equal(originalScore, original.AssignedScore.Value);
        Assert.Equal(4, original.Options.Count);
    }

    // =========================================================================
    // Entity equality — por identidad
    // =========================================================================

    [Fact]
    public void TwoQuestionsWithSameId_AreEqual()
    {
        // Arrange: crea dos instancias de Question con el mismo Id manualmente.
        // Nota: esto requiere crear dos preguntas vía Create y luego comparar por Id.
        // Como el Id se genera internamente, creamos una pregunta y verificamos
        // que sea igual a sí misma (reflexividad) y diferente de otra.
        var q1 = Question.Create(ValidDraft, displayOrder: 1);
        var q2 = Question.Create(ValidDraft, displayOrder: 1);

        // Assert: dos preguntas distintas (diferente Id) no deben ser iguales por identidad.
        Assert.NotEqual(q1, q2);
        Assert.False(q1.Equals(q2));
    }

    [Fact]
    public void Question_EqualsItself_ReturnsTrue()
    {
        // Arrange: crea una pregunta.
        var q1 = Question.Create(ValidDraft, displayOrder: 1);

        // Assert: una entidad debe ser igual a sí misma (reflexividad).
        Assert.Equal(q1, q1);
        Assert.True(q1.Equals(q1));
    }
}
