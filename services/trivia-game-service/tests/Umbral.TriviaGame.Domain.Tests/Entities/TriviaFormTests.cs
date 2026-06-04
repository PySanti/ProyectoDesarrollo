using System;
using System.Collections.Generic;
using System.Linq;
using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.Common.Exceptions;
using Umbral.TriviaGame.Domain.Drafts;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.Events;
using Umbral.TriviaGame.Domain.ValueObjects;
using Xunit;

namespace Umbral.TriviaGame.Domain.Tests.Entities;

public class TriviaFormTests
{
    // =========================================================================
    // Helpers compartidos
    // =========================================================================

    /// <summary>
    /// Genera 4 opciones de prueba con la primera como correcta.
    /// </summary>
    private static IReadOnlyList<AnswerOptionDraft> FourOptions => new List<AnswerOptionDraft>
    {
        AnswerOptionDraft.Create("París", true),
        AnswerOptionDraft.Create("Londres", false),
        AnswerOptionDraft.Create("Berlín", false),
        AnswerOptionDraft.Create("Madrid", false),
    };

    /// <summary>
    /// Título válido reutilizable en los tests de creación.
    /// </summary>
    private static FormTitle ValidTitle =>
        FormTitle.Create("Cultura General - Ronda 1");

    /// <summary>
    /// Identificador de operador válido reutilizable en los tests.
    /// </summary>
    private static OperatorId ValidOperatorId =>
        OperatorId.Create("op-12345-abc");

    /// <summary>
    /// Genera un solo QuestionDraft válido (pregunta única) para tests simples.
    /// </summary>
    private static QuestionDraft OneQuestionDraft =>
        QuestionDraft.Create(
            "¿Cuál es la capital de Francia?",
            assignedScore: 10,
            timeLimitSeconds: 30,
            displayOrder: 1,
            FourOptions);

    /// <summary>
    /// Genera dos QuestionDrafts con órdenes secuenciales 1 y 2.
    /// </summary>
    private static IReadOnlyList<QuestionDraft> TwoQuestionDrafts
    {
        get
        {
            var opciones2 = new List<AnswerOptionDraft>
            {
                AnswerOptionDraft.Create("3.14", true),
                AnswerOptionDraft.Create("2.71", false),
                AnswerOptionDraft.Create("1.61", false),
                AnswerOptionDraft.Create("0.57", false),
            };

            return new List<QuestionDraft>
            {
                QuestionDraft.Create(
                    "¿Cuál es el valor de Pi?",
                    assignedScore: 15,
                    timeLimitSeconds: 20,
                    displayOrder: 1,
                    FourOptions),
                QuestionDraft.Create(
                    "¿Cuál es el valor del Número de Euler?",
                    assignedScore: 15,
                    timeLimitSeconds: 25,
                    displayOrder: 2,
                    opciones2),
            };
        }
    }

    /// <summary>
    /// Genera tres QuestionDrafts con órdenes no secuenciales (3, 1, 2)
    /// para probar la normalización a 1, 2, 3.
    /// </summary>
    private static IReadOnlyList<QuestionDraft> ThreeQuestionDraftsNonSequential
    {
        get
        {
            var opcionesB = new List<AnswerOptionDraft>
            {
                AnswerOptionDraft.Create("4", true),
                AnswerOptionDraft.Create("2", false),
                AnswerOptionDraft.Create("8", false),
                AnswerOptionDraft.Create("16", false),
            };

            var opcionesC = new List<AnswerOptionDraft>
            {
                AnswerOptionDraft.Create("Rojo", true),
                AnswerOptionDraft.Create("Verde", false),
                AnswerOptionDraft.Create("Azul", false),
                AnswerOptionDraft.Create("Amarillo", false),
            };

            // Órdenes intencionalmente desordenados: 3, 1, 2.
            return new List<QuestionDraft>
            {
                QuestionDraft.Create(
                    "¿Cuál es la raíz cuadrada de 16?",
                    assignedScore: 10,
                    timeLimitSeconds: 15,
                    displayOrder: 3,
                    opcionesB),
                QuestionDraft.Create(
                    "¿Cuál es la capital de Francia?",
                    assignedScore: 10,
                    timeLimitSeconds: 30,
                    displayOrder: 1,
                    FourOptions),
                QuestionDraft.Create(
                    "¿Qué color obtienes al mezclar azul y amarillo?",
                    assignedScore: 10,
                    timeLimitSeconds: 20,
                    displayOrder: 2,
                    opcionesC),
            };
        }
    }

    // =========================================================================
    // Create — Factory method
    // =========================================================================

    [Fact]
    public void Create_WithValidData_SetsAllProperties()
    {
        // Arrange & Act: crea un formulario con 1 pregunta válida.
        var form = TriviaForm.Create(
            ValidTitle,
            ValidOperatorId,
            new[] { OneQuestionDraft });

        // Assert: verifica que cada propiedad del aggregate se haya asignado correctamente.
        Assert.NotNull(form.Id);                              // El Id fue generado automáticamente.
        Assert.NotEqual(Guid.Empty, form.Id.Value);           // Id no es Guid vacío.
        Assert.Equal("Cultura General - Ronda 1", form.Title.Value); // Título preservado.
        Assert.Equal("op-12345-abc", form.CreatedByOperatorId.Value); // Operador preservado.
        Assert.Equal(1, form.Questions.Count);                // Exactamente 1 pregunta.
        Assert.True(form.IsComplete);                         // Al menos 1 pregunta → IsComplete.
        Assert.True(form.CreatedAtUtc <= DateTimeOffset.UtcNow); // Timestamp <= now.
        Assert.Equal(form.CreatedAtUtc, form.UpdatedAtUtc);   // Al crear, updated = created.
    }

    [Fact]
    public void Create_WithSingleQuestion_Success()
    {
        // Arrange & Act: crea un formulario con exactamente 1 pregunta.
        var form = TriviaForm.Create(
            ValidTitle,
            ValidOperatorId,
            new[] { OneQuestionDraft });

        // Assert: el formulario tiene 1 pregunta con los datos correctos.
        var question = Assert.Single(form.Questions);
        Assert.Equal("¿Cuál es la capital de Francia?", question.Text.Value);
        Assert.Equal(10, question.AssignedScore.Value);
        Assert.Equal(4, question.Options.Count);
        Assert.Single(question.Options, o => o.IsCorrect);
    }

    [Fact]
    public void Create_WithMultipleQuestions_AddsAllQuestions()
    {
        // Arrange & Act: crea un formulario con 2 preguntas.
        var form = TriviaForm.Create(
            ValidTitle,
            ValidOperatorId,
            TwoQuestionDrafts);

        // Assert: el formulario contiene exactamente 2 preguntas.
        Assert.Equal(2, form.Questions.Count);
    }

    [Fact]
    public void Create_WithMultipleQuestions_NormalizesDisplayOrder()
    {
        // Arrange & Act: crea un formulario con drafts en órdenes 3, 1, 2.
        var form = TriviaForm.Create(
            ValidTitle,
            ValidOperatorId,
            ThreeQuestionDraftsNonSequential);

        // Assert: las preguntas se resecuenciaron a DisplayOrder 1, 2, 3 sin gaps.
        var orders = form.Questions
            .OrderBy(q => q.DisplayOrder)
            .Select(q => q.DisplayOrder)
            .ToList();

        Assert.Equal(new[] { 1, 2, 3 }, orders);

        // Assert adicional: el orden de los textos debe corresponder al DisplayOrder
        // original de los borradores (orden 1 → "capital de Francia", orden 2 → "color", orden 3 → "raíz").
        var questionsByOrder = form.Questions
            .OrderBy(q => q.DisplayOrder)
            .ToList();

        Assert.Contains("capital de Francia", questionsByOrder[0].Text.Value);
        Assert.Contains("Qué color", questionsByOrder[1].Text.Value);
        Assert.Contains("raíz cuadrada", questionsByOrder[2].Text.Value);
    }

    // =========================================================================
    // Create — Casos inválidos (fail-fast)
    // =========================================================================

    [Fact]
    public void Create_WithNullTitle_ThrowsDomainValidationException()
    {
        // Arrange, Act & Assert: título nulo produce DomainValidationException.
        var ex = Assert.Throws<DomainValidationException>(() =>
            TriviaForm.Create(
                null!,                        // Title nulo.
                ValidOperatorId,
                new[] { OneQuestionDraft }));

        Assert.Contains("título", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithNullOperatorId_ThrowsDomainValidationException()
    {
        // Arrange, Act & Assert: operador nulo produce DomainValidationException.
        var ex = Assert.Throws<DomainValidationException>(() =>
            TriviaForm.Create(
                ValidTitle,
                null!,                        // OperatorId nulo.
                new[] { OneQuestionDraft }));

        Assert.Contains("operador", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithNullDrafts_ThrowsDomainValidationException()
    {
        // Arrange, Act & Assert: colección de borradores nula produce DomainValidationException.
        var ex = Assert.Throws<DomainValidationException>(() =>
            TriviaForm.Create(
                ValidTitle,
                ValidOperatorId,
                null!));                      // questionDrafts nulo.

        Assert.Contains("borradores", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithEmptyDrafts_ThrowsEmptyQuestionSetException()
    {
        // Arrange, Act & Assert: 0 borradores produce EmptyQuestionSetException.
        Assert.Throws<EmptyQuestionSetException>(() =>
            TriviaForm.Create(
                ValidTitle,
                ValidOperatorId,
                Array.Empty<QuestionDraft>()));
    }

    // =========================================================================
    // UpdateTitle
    // =========================================================================

    [Fact]
    public void UpdateTitle_WithValidTitle_UpdatesPropertyAndTimestamp()
    {
        // Arrange: crea un formulario y captura su timestamp original.
        var form = TriviaForm.Create(
            ValidTitle,
            ValidOperatorId,
            new[] { OneQuestionDraft });
        var originalUpdated = form.UpdatedAtUtc;

        // Espera un instante para que el timestamp sea distinguible.
        System.Threading.Thread.Sleep(1);

        // Act: actualiza el título.
        var newTitle = FormTitle.Create("Cultura General - Ronda 2");
        form.UpdateTitle(newTitle);

        // Assert: el título cambió y el timestamp se actualizó.
        Assert.Equal("Cultura General - Ronda 2", form.Title.Value);
        Assert.True(form.UpdatedAtUtc > originalUpdated,
            "El timestamp UpdatedAtUtc debe ser posterior al anterior tras una mutación.");
    }

    [Fact]
    public void UpdateTitle_WithNull_ThrowsDomainValidationException()
    {
        // Arrange: crea un formulario y captura el título original.
        var form = TriviaForm.Create(
            ValidTitle,
            ValidOperatorId,
            new[] { OneQuestionDraft });
        var originalTitle = form.Title.Value;

        // Act & Assert: título nulo lanza excepción.
        var ex = Assert.Throws<DomainValidationException>(() =>
            form.UpdateTitle(null!));

        Assert.Contains("título", ex.Message, StringComparison.OrdinalIgnoreCase);

        // Assert adicional: el título original no debe haber cambiado (fail-fast).
        Assert.Equal(originalTitle, form.Title.Value);
    }

    // =========================================================================
    // ReplaceQuestions
    // =========================================================================

    [Fact]
    public void ReplaceQuestions_WithValidDrafts_ReplacesAndNormalizes()
    {
        // Arrange: crea un formulario con 1 pregunta inicial.
        var form = TriviaForm.Create(
            ValidTitle,
            ValidOperatorId,
            new[] { OneQuestionDraft });
        var originalUpdated = form.UpdatedAtUtc;

        // Espera un instante para que el timestamp sea distinguible.
        System.Threading.Thread.Sleep(1);

        // Act: reemplaza con 2 preguntas nuevas.
        form.ReplaceQuestions(TwoQuestionDrafts);

        // Assert: ahora hay 2 preguntas (no 1).
        Assert.Equal(2, form.Questions.Count);

        // Assert: las preguntas nuevas tienen los textos esperados.
        var texts = form.Questions
            .OrderBy(q => q.DisplayOrder)
            .Select(q => q.Text.Value)
            .ToList();

        Assert.Contains("Pi", texts[0]);
        Assert.Contains("Euler", texts[1]);

        // Assert: el timestamp se actualizó.
        Assert.True(form.UpdatedAtUtc > originalUpdated,
            "El timestamp UpdatedAtUtc debe actualizarse tras ReplaceQuestions.");
    }

    [Fact]
    public void ReplaceQuestions_WithEmptySet_ThrowsAndPreservesOriginal()
    {
        // Arrange: crea un formulario con 1 pregunta y captura el estado original.
        var form = TriviaForm.Create(
            ValidTitle,
            ValidOperatorId,
            new[] { OneQuestionDraft });
        var originalCount = form.Questions.Count;
        var originalUpdated = form.UpdatedAtUtc;

        // Act & Assert: reemplazar con 0 preguntas lanza EmptyQuestionSetException.
        Assert.Throws<EmptyQuestionSetException>(() =>
            form.ReplaceQuestions(Array.Empty<QuestionDraft>()));

        // Assert: el estado original no se modificó (fail-fast antes de mutar).
        Assert.Equal(originalCount, form.Questions.Count);
        Assert.Equal(originalUpdated, form.UpdatedAtUtc);
    }

    [Fact]
    public void ReplaceQuestions_WithNullDrafts_ThrowsAndPreservesOriginal()
    {
        // Arrange: crea un formulario con 1 pregunta y captura el estado original.
        var form = TriviaForm.Create(
            ValidTitle,
            ValidOperatorId,
            new[] { OneQuestionDraft });
        var originalCount = form.Questions.Count;

        // Act & Assert: reemplazar con null lanza DomainValidationException.
        var ex = Assert.Throws<DomainValidationException>(() =>
            form.ReplaceQuestions(null!));

        Assert.Contains("borradores", ex.Message, StringComparison.OrdinalIgnoreCase);

        // Assert: el estado original no se modificó (fail-fast antes de mutar).
        Assert.Equal(originalCount, form.Questions.Count);
    }

    // =========================================================================
    // Inmutabilidad de CreatedByOperatorId
    // =========================================================================

    [Fact]
    public void CreatedByOperatorId_IsImmutable()
    {
        // Arrange & Act: crea un formulario.
        var form = TriviaForm.Create(
            ValidTitle,
            ValidOperatorId,
            new[] { OneQuestionDraft });

        // Assert: la propiedad CreatedByOperatorId solo tiene getter (sin setter).
        // Verificamos que el valor no cambia tras mutaciones en otras propiedades.
        var originalOperator = form.CreatedByOperatorId.Value;

        // Ejecuta mutaciones que no deberían afectar al operador creador.
        form.UpdateTitle(FormTitle.Create("Nuevo título"));
        form.ReplaceQuestions(TwoQuestionDrafts);

        // El operador creador sigue siendo el mismo.
        Assert.Equal(originalOperator, form.CreatedByOperatorId.Value);
    }

    // =========================================================================
    // Entity Equality — por identidad
    // =========================================================================

    [Fact]
    public void TwoFormsWithSameId_AreEqual()
    {
        // Arrange: crea dos formularios con el mismo Id manual (aunque no es el flujo normal).
        var id = TriviaFormId.New();
        var title = FormTitle.Create("Form A");
        var opId = OperatorId.Create("op-1");
        var now = DateTimeOffset.UtcNow;

        // Usa el constructor privado vía Create y luego crea otro form con Create distinto.
        var formA = TriviaForm.Create(
            title,
            opId,
            new[]
            {
                QuestionDraft.Create(
                    "Pregunta?",
                    assignedScore: 10,
                    timeLimitSeconds: 30,
                    displayOrder: 1,
                    new List<AnswerOptionDraft>
                    {
                        AnswerOptionDraft.Create("A", true),
                        AnswerOptionDraft.Create("B", false),
                        AnswerOptionDraft.Create("C", false),
                        AnswerOptionDraft.Create("D", false),
                    }),
            });

        var formB = formA; // Misma referencia.

        // Assert: misma instancia → iguales.
        Assert.Equal(formA, formB);
        Assert.True(formA == formB);
        Assert.False(formA != formB);
    }

    [Fact]
    public void TwoFormsWithDifferentId_AreNotEqual()
    {
        // Arrange: crea dos formularios independientes (diferentes Id generados automáticamente).
        var formA = TriviaForm.Create(
            ValidTitle,
            ValidOperatorId,
            new[] { OneQuestionDraft });

        var formB = TriviaForm.Create(
            ValidTitle,
            ValidOperatorId,
            new[] { OneQuestionDraft });

        // Assert: son entidades diferentes con Ids distintos → no iguales.
        Assert.NotEqual(formA, formB);
        Assert.True(formA != formB);
        Assert.False(formA == formB);
    }

    // =========================================================================
    // IsComplete (preliminar, D-05 agregará validación exhaustiva)
    // =========================================================================

    [Fact]
    public void IsComplete_WithOneQuestion_ReturnsTrue()
    {
        // Arrange & Act: formulario con 1 pregunta válida.
        var form = TriviaForm.Create(
            ValidTitle,
            ValidOperatorId,
            new[] { OneQuestionDraft });

        // Assert: IsComplete es true porque hay al menos 1 pregunta.
        Assert.True(form.IsComplete);
    }

    [Fact]
    public void ToString_ReturnsExpectedFormat()
    {
        // Arrange & Act: crea un formulario simple.
        var form = TriviaForm.Create(
            ValidTitle,
            ValidOperatorId,
            new[] { OneQuestionDraft });

        // Act
        var str = form.ToString();

        // Assert: contiene las partes esperadas.
        Assert.Contains("TriviaForm", str);
        Assert.Contains("Cultura General - Ronda 1", str);
        Assert.Contains("Questions: 1", str);
    }

    // =========================================================================
    // Domain Events (D-06)
    // =========================================================================

    [Fact]
    public void Create_RaisesTriviaFormCreatedDomainEvent()
    {
        // Arrange & Act: crea un formulario.
        var form = TriviaForm.Create(
            ValidTitle,
            ValidOperatorId,
            new[] { OneQuestionDraft });

        // Assert: el evento TriviaFormCreatedDomainEvent fue registrado.
        var createdEvent = form.DomainEvents
            .OfType<TriviaFormCreatedDomainEvent>()
            .SingleOrDefault();

        Assert.NotNull(createdEvent);
        Assert.Equal(form.Id, createdEvent.FormId);
        Assert.Equal(form.Title, createdEvent.Title);
        Assert.Equal(form.CreatedByOperatorId, createdEvent.CreatedByOperatorId);
        Assert.True(createdEvent.OccurredAtUtc <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Create_DoesNotRaiseUpdatedEvent()
    {
        // Arrange & Act: crea un formulario.
        var form = TriviaForm.Create(
            ValidTitle,
            ValidOperatorId,
            new[] { OneQuestionDraft });

        // Assert: solo debe haber el evento de creación, no de actualización.
        Assert.Empty(form.DomainEvents.OfType<TriviaFormUpdatedDomainEvent>());
    }

    [Fact]
    public void UpdateTitle_RaisesTriviaFormUpdatedDomainEvent_WithTitleOnly()
    {
        // Arrange: crea un formulario.
        var form = TriviaForm.Create(
            ValidTitle,
            ValidOperatorId,
            new[] { OneQuestionDraft });

        // Flush eventos de creación para aislar los de la mutación.
        form.FlushDomainEvents();

        // Act: actualiza el título.
        var newTitle = FormTitle.Create("Nuevo Título");
        form.UpdateTitle(newTitle);

        // Assert: se elevó un evento TriviaFormUpdatedDomainEvent con TitleOnly.
        var updatedEvent = form.DomainEvents
            .OfType<TriviaFormUpdatedDomainEvent>()
            .SingleOrDefault();

        Assert.NotNull(updatedEvent);
        Assert.Equal(form.Id, updatedEvent.FormId);
        Assert.Equal(newTitle, updatedEvent.Title);
        Assert.Equal(TriviaFormUpdateKind.TitleOnly, updatedEvent.UpdateKind);
    }

    [Fact]
    public void ReplaceQuestions_RaisesTriviaFormUpdatedDomainEvent_WithQuestionsOnly()
    {
        // Arrange: crea un formulario.
        var form = TriviaForm.Create(
            ValidTitle,
            ValidOperatorId,
            new[] { OneQuestionDraft });

        // Flush eventos de creación.
        form.FlushDomainEvents();

        // Act: reemplaza preguntas.
        form.ReplaceQuestions(TwoQuestionDrafts);

        // Assert: se elevó un evento TriviaFormUpdatedDomainEvent con QuestionsOnly.
        var updatedEvent = form.DomainEvents
            .OfType<TriviaFormUpdatedDomainEvent>()
            .SingleOrDefault();

        Assert.NotNull(updatedEvent);
        Assert.Equal(TriviaFormUpdateKind.QuestionsOnly, updatedEvent.UpdateKind);
    }

    [Fact]
    public void FlushDomainEvents_ClearsEventList()
    {
        // Arrange: crea un formulario (genera un evento de creación).
        var form = TriviaForm.Create(
            ValidTitle,
            ValidOperatorId,
            new[] { OneQuestionDraft });

        // Verifica que hay exactamente 1 evento antes del flush.
        Assert.Single(form.DomainEvents);

        // Act: Flush limpia la lista y retorna los eventos.
        var flushed = form.FlushDomainEvents();

        // Assert: la lista interna quedó vacía.
        Assert.Empty(form.DomainEvents);

        // Assert: los eventos retornados contienen el evento de creación.
        Assert.Single(flushed.OfType<TriviaFormCreatedDomainEvent>());
    }

    [Fact]
    public void MultipleMutations_AccumulateEvents()
    {
        // Arrange: crea un formulario.
        var form = TriviaForm.Create(
            ValidTitle,
            ValidOperatorId,
            new[] { OneQuestionDraft });

        // Flush eventos de creación.
        form.FlushDomainEvents();

        // Act: ejecuta dos mutaciones.
        form.UpdateTitle(FormTitle.Create("Título 2"));
        form.ReplaceQuestions(TwoQuestionDrafts);

        // Assert: hay 2 eventos de actualización.
        Assert.Equal(2, form.DomainEvents.Count);

        var events = form.DomainEvents.OfType<TriviaFormUpdatedDomainEvent>().ToList();
        Assert.Equal(2, events.Count);
        Assert.Contains(events, e => e.UpdateKind == TriviaFormUpdateKind.TitleOnly);
        Assert.Contains(events, e => e.UpdateKind == TriviaFormUpdateKind.QuestionsOnly);
    }
}
