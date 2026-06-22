# HU-15 — Design: Crear formularios de Trivia

## Overview

| Aspecto | Decisión |
| --- | --- |
| Owning service | Trivia Game Service |
| Supporting services | Identity Service (autenticación JWT / rol Operador vía Keycloak; sin llamada HTTP síncrona obligatoria si el token ya incluye roles) |
| Client | React web (panel operador) |
| Architecture | Clean Architecture / Hexagonal |
| Application style | CQRS + MediatR |
| Persistence | PostgreSQL + EF Core |
| Real-time | No aplica |
| Async messaging | No aplica en esta HU |

## Bounded context

**Trivia Context** — subdominio de plantillas de contenido (`TriviaForm` aggregate) dentro de `Umbral.TriviaGame` (nombre de solución orientativo).

## Domain model (DDD — C# .NET 8)

Nombres de clases, propiedades y métodos en **inglés**. Entidades con **setters privados**; mutaciones vía métodos de dominio o factory.

### Aggregate root: `TriviaForm`

```csharp
public sealed class TriviaForm : AggregateRoot<TriviaFormId>
{
    public FormTitle Title { get; private set; }
    public OperatorId CreatedByOperatorId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    private readonly List<Question> _questions = new();
    public IReadOnlyCollection<Question> Questions => _questions.AsReadOnly();

    public bool IsComplete => TriviaFormCompletenessValidator.IsComplete(this);

    public static TriviaForm Create(FormTitle title, OperatorId operatorId, IEnumerable<QuestionDraft> questionDrafts);
    public void UpdateTitle(FormTitle title);
    public void ReplaceQuestions(IEnumerable<QuestionDraft> questionDrafts);
}
```

**Invariants (aggregate level):**

- `Title` must be non-empty (enforced by `FormTitle` VO).
- Must contain at least one `Question` after create/update operations that persist a form.
- Questions are ordered by `DisplayOrder` starting at 1 without gaps after normalization.
- `CreatedByOperatorId` is immutable after creation.

### Entity: `Question`

```csharp
public sealed class Question : Entity<QuestionId>
{
    public QuestionText Text { get; private set; }
    public AssignedScore AssignedScore { get; private set; }
    public TimeLimit TimeLimit { get; private set; }
    public int DisplayOrder { get; private set; }
    private readonly List<AnswerOption> _options = new();
    public IReadOnlyCollection<AnswerOption> Options => _options.AsReadOnly();

    internal static Question Create(QuestionDraft draft, int displayOrder);
    internal void UpdateFrom(QuestionDraft draft, int displayOrder);
    public AnswerOption GetCorrectOption();
    public bool HasExactlyOneCorrectOption();
}
```

**Invariants (question level):**

- `Text` non-empty.
- Exactly **4** `AnswerOption` instances.
- Exactly **1** option with `IsCorrect = true`.
- `AssignedScore.Value` in range `[1, 1000]`.
- `TimeLimit.Seconds` in range `[5, 300]`.
- Incorrect options implicitly award **0** points at gameplay time; no per-option score is stored.

### Value object: `AnswerOption`

```csharp
public sealed class AnswerOption : ValueObject
{
    public OptionText Text { get; private init; }
    public bool IsCorrect { get; private init; }

    public static AnswerOption Create(OptionText text, bool isCorrect);
}
```

Options are value objects embedded in `Question`; identity within a question is positional (index 0–3) or stable `OptionKey` GUID generated on persist.

### Value objects

| Type | Properties | Validation |
| --- | --- | --- |
| `TriviaFormId` | `Guid Value` | Non-empty GUID |
| `QuestionId` | `Guid Value` | Non-empty GUID |
| `FormTitle` | `string Value` | Not null/whitespace; max 200 chars |
| `QuestionText` | `string Value` | Not null/whitespace; max 1000 chars |
| `OptionText` | `string Value` | Not null/whitespace; max 500 chars |
| `AssignedScore` | `int Value` | 1..1000 |
| `TimeLimit` | `int Seconds` | 5..300 |
| `OperatorId` | `string Value` | Keycloak subject / local operator reference |

### Domain service: `TriviaFormCompletenessValidator`

Static or injectable domain service implementing RF-16 / TRIVIA-FORM-001:

```csharp
public static class TriviaFormCompletenessValidator
{
    public static bool IsComplete(TriviaForm form);
    public static IReadOnlyList<string> GetIncompleteReasons(TriviaForm form);
}
```

**Completeness rules:**

1. At least one question.
2. Each question satisfies all question-level invariants.
3. No duplicate empty option texts within the same question (optional quality rule: all four option texts must be distinct — enforced if texts collide case-insensitively).

Returns `false` when any rule fails; used to populate response field `isComplete`.

### Domain exceptions

| Exception | When |
| --- | --- |
| `InvalidQuestionOptionsCountException` | Options count ≠ 4 |
| `InvalidCorrectOptionCountException` | Correct options ≠ 1 |
| `InvalidAssignedScoreException` | Score out of range |
| `InvalidTimeLimitException` | Seconds out of range |
| `EmptyQuestionSetException` | Zero questions on persist |
| `TriviaFormNotFoundException` | Lookup miss (mapped to 404 in API) |

### Domain events (in-process)

| Event | Trigger | Notes |
| --- | --- | --- |
| `TriviaFormCreatedDomainEvent` | After successful create | Optional audit trail inside service; no RabbitMQ in HU-15 |
| `TriviaFormUpdatedDomainEvent` | After successful update | Same as above |

## Application layer (CQRS + MediatR)

### Commands

#### `CreateTriviaFormCommand`

```csharp
public sealed record CreateTriviaFormCommand(
    string Title,
    IReadOnlyList<QuestionInputDto> Questions
) : IRequest<TriviaFormDetailDto>;

public sealed record QuestionInputDto(
    string Text,
    int AssignedScore,
    int TimeLimitSeconds,
    int DisplayOrder,
    IReadOnlyList<AnswerOptionInputDto> Options
);

public sealed record AnswerOptionInputDto(
    string Text,
    bool IsCorrect
);
```

**Handler flow:**

1. Validate command (FluentValidation).
2. Map DTOs → `QuestionDraft` value objects.
3. `TriviaForm.Create(...)`.
4. Persist via `ITriviaFormRepository`.
5. Return `TriviaFormDetailDto`.

#### `UpdateTriviaFormCommand`

```csharp
public sealed record UpdateTriviaFormCommand(
    Guid FormId,
    string Title,
    IReadOnlyList<QuestionInputDto> Questions
) : IRequest<TriviaFormDetailDto>;
```

**Handler flow:**

1. Load aggregate by id; 404 if missing.
2. Validate command.
3. `form.UpdateTitle(...)` + `form.ReplaceQuestions(...)`.
4. Persist; return DTO.

### Queries

#### `GetTriviaFormByIdQuery`

```csharp
public sealed record GetTriviaFormByIdQuery(Guid FormId) : IRequest<TriviaFormDetailDto?>;
```

Read-only; no state change. Returns `null` → 404 at API layer.

### Response DTO: `TriviaFormDetailDto`

```csharp
public sealed record TriviaFormDetailDto(
    Guid Id,
    string Title,
    bool IsComplete,
    IReadOnlyList<string> IncompleteReasons,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<QuestionDetailDto> Questions
);

public sealed record QuestionDetailDto(
    Guid Id,
    string Text,
    int AssignedScore,
    int TimeLimitSeconds,
    int DisplayOrder,
    IReadOnlyList<AnswerOptionDetailDto> Options
);

public sealed record AnswerOptionDetailDto(
    int Index,
    string Text,
    bool IsCorrect
);
```

### Validators (FluentValidation)

- `CreateTriviaFormCommandValidator`
- `UpdateTriviaFormCommandValidator`

Mirror domain rules at application boundary for early rejection and clear API errors.

### Repository port

```csharp
public interface ITriviaFormRepository
{
    Task AddAsync(TriviaForm form, CancellationToken cancellationToken);
    Task<TriviaForm?> GetByIdAsync(TriviaFormId id, CancellationToken cancellationToken);
    Task UpdateAsync(TriviaForm form, CancellationToken cancellationToken);
}
```

## Infrastructure layer

### EF Core entities (persistence model — separate from domain)

| Table | Key columns |
| --- | --- |
| `trivia_forms` | `id`, `title`, `created_by_operator_id`, `created_at_utc`, `updated_at_utc` |
| `trivia_questions` | `id`, `form_id`, `text`, `assigned_score`, `time_limit_seconds`, `display_order` |
| `trivia_answer_options` | `id`, `question_id`, `option_index`, `text`, `is_correct` |

- Owned types or separate tables for options (prefer separate table for querying clarity).
- Cascade delete questions/options when form deleted (delete not exposed in HU-15 API).
- `TriviaFormConfiguration`, `QuestionConfiguration`, `AnswerOptionConfiguration` in Infrastructure; no business rules in configurations beyond constraints mirroring domain.

### Mapping

- `TriviaFormMapper` / `QuestionMapper` between domain aggregates and persistence entities.
- Domain remains free of EF attributes.

## API layer

Base path: `/api/trivia/forms`
Authorization policy: `RequireRole("Operador")` (exact claim mapping aligned with Identity Service / Keycloak).

| Method | Path | MediatR | Success | Errors |
| --- | --- | --- | --- | --- |
| POST | `/api/trivia/forms` | `CreateTriviaFormCommand` | 201 + body | 400, 401, 403 |
| PUT | `/api/trivia/forms/{formId}` | `UpdateTriviaFormCommand` | 200 + body | 400, 401, 403, 404 |
| GET | `/api/trivia/forms/{formId}` | `GetTriviaFormByIdQuery` | 200 + body | 401, 403, 404 |

`TriviaFormsController` delegates to `IMediator`; no business logic in controller.

## HTTP contracts (to document in `contracts/http/trivia-game-api.md`)

### POST `/api/trivia/forms`

**Related HU:** HU-15
**Related requirement:** RF-15, RF-16
**Authorization:** Operador

**Request:**

```json
{
  "title": "General Knowledge Round 1",
  "questions": [
    {
      "text": "What is the capital of France?",
      "assignedScore": 10,
      "timeLimitSeconds": 30,
      "displayOrder": 1,
      "options": [
        { "text": "Paris", "isCorrect": true },
        { "text": "London", "isCorrect": false },
        { "text": "Berlin", "isCorrect": false },
        { "text": "Madrid", "isCorrect": false }
      ]
    }
  ]
}
```

**Response 201:**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "General Knowledge Round 1",
  "isComplete": true,
  "incompleteReasons": [],
  "createdAtUtc": "2026-05-30T12:00:00Z",
  "updatedAtUtc": "2026-05-30T12:00:00Z",
  "questions": []
}
```

**Error responses:**

| Status | Reason |
| --- | --- |
| 400 | Validation failure (options count, correct count, score, timer, empty title) |
| 401 | Unauthenticated |
| 403 | Not Operador |

**Business rules:** BR-T01, BR-T02, HU-15-FORM-001..006, TRIVIA-SCORE-003
**Events published:** none
**Real-time updates:** none

### PUT `/api/trivia/forms/{formId}`

Same request/response shape as POST; 200 on success; 404 if form not found.

### GET `/api/trivia/forms/{formId}`

**Related requirement:** RF-15, RF-35
**Authorization:** Operador
**Response 200:** same as `TriviaFormDetailDto` JSON above.
**Events published:** none
**Real-time updates:** none

## Frontend design (React web)

### Routes (orientative)

| Route | Purpose |
| --- | --- |
| `/operator/trivia/forms/new` | Create form |
| `/operator/trivia/forms/:formId/edit` | Edit form |
| `/operator/trivia/forms/:formId` | View detail (read-only mode or shared with edit) |

### Components

| Component | Responsibility |
| --- | --- |
| `TriviaFormEditorPage` | Orchestrates create/edit flow |
| `TriviaFormTitleField` | Title input with validation |
| `QuestionListEditor` | Add/remove/reorder questions |
| `QuestionEditor` | Text, score, timer, 4 option fields, single correct selector (radio) |
| `TriviaFormCompletenessBadge` | Shows `isComplete` after save/load |

### Client validation (usability only)

- Enforce 4 options in UI before submit.
- Enforce single correct option via radio group.
- Backend remains source of truth.

### API client

- `triviaFormsApi.create(payload)`
- `triviaFormsApi.update(formId, payload)`
- `triviaFormsApi.getById(formId)`

## Scoring and timer decision (explicit)

At form design time and in stored model:

```txt
scoreEarnedOnCorrectAnswer = question.AssignedScore
scoreEarnedOnIncorrectAnswer = 0
timeDoesNotModifyScore = true
```

Rejected formula (must not appear in form model or UI):

```txt
scoreEarned = assignedScore * (remainingTime / totalTime)
```

`TimeLimit` is persisted for gameplay synchronization (HU-24+) only.

## Design patterns

| Pattern | Application |
| --- | --- |
| Aggregate Root | `TriviaForm` protects question/option invariants |
| Value Object | Titles, scores, time limits, option text |
| Factory method | `TriviaForm.Create`, `Question.Create` |
| Domain service | `TriviaFormCompletenessValidator` |
| CQRS | Separate commands and queries |
| Repository | `ITriviaFormRepository` |
| Specification | Completeness rules encapsulated in validator |

## Tests required

### Domain unit tests

- Create form with valid structure → success, `IsComplete = true`.
- Reject question with 3 or 5 options.
- Reject 0 or 2+ correct options.
- Reject score ≤ 0 or time ≤ 0.
- Reject empty question set.
- `GetCorrectOption()` returns the single correct option.
- Completeness validator lists reasons when incomplete.

### Application unit tests

- Validators reject malformed commands.
- Handlers call repository; map exceptions to results.

### Integration tests

- POST create persists round-trip GET.
- PUT update modifies persisted data.
- GET returns 404 for unknown id.
- Authorization 403 for non-operator token.

### Frontend tests (minimum)

- Question editor renders exactly 4 option slots.
- Only one correct option selectable.
- Displays API validation errors.

## Security

- JWT bearer authentication.
- Role policy `Operador` on all endpoints.
- No participant or administrator access to form mutation endpoints in this HU.

## Observability

- Structured logs on create/update with `formId`, `operatorId`.
- No PII beyond operator subject id in logs.

## Dependencies for downstream HUs

| HU | Dependency on HU-15 |
| --- | --- |
| HU-17 | Requires `isComplete = true` on associated form |
| HU-24+ | Reads question order, timers, options, correct flag, assigned scores from persisted form |
