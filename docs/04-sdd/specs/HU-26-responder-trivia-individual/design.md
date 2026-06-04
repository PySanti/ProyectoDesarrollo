# HU-26 — Design: Responder Trivia individual

## Overview

| Aspecto | Decisión |
| --- | --- |
| Owning service | Trivia Game Service |
| Supporting services | Identity Service (JWT / autenticación del participante) |
| Client | React Native mobile (frontend congelado — solo backend) |
| Architecture | Clean Architecture / Hexagonal |
| Application style | CQRS + MediatR |
| Persistence | PostgreSQL + EF Core |
| Real-time | SignalR (notificar cierre de pregunta) |
| Async messaging | No aplica en esta HU |

## Bounded context

**Trivia Context** — subdominio de gameplay (`PartidaTrivia` aggregate). Se introduce nueva entidad `RespuestaTrivia` y se extiende el agregado `PartidaTrivia` con tracking de pregunta activa y respuestas.

## Domain model (DDD — C# .NET 8)

### Cambios sobre domain existente

Se agregan:
1. Nueva entidad `RespuestaTrivia`
2. Seguimiento de pregunta activa en `PartidaTrivia` (`PreguntaActualId`, estado de pregunta, respuestas registradas)
3. Nuevo evento de dominio `RespuestaTriviaRegistradaDomainEvent`
4. Nuevo value object `RespuestaTriviaId`

### New entity: `RespuestaTrivia`

```csharp
public sealed class RespuestaTrivia : Entity<RespuestaTriviaId>
{
    public PartidaId PartidaId { get; private set; }
    public QuestionId PreguntaId { get; private set; }
    public string UsuarioId { get; private set; }
    public int OpcionSeleccionadaIndex { get; private set; }
    public bool EsCorrecta { get; private set; }
    public int PuntajeObtenido { get; private set; }
    public TimeSpan TiempoEmpleado { get; private set; }
    public DateTimeOffset FechaRespuesta { get; private set; }

    private RespuestaTrivia() : base(RespuestaTriviaId.New()) { }

    public static RespuestaTrivia Create(
        PartidaId partidaId,
        QuestionId preguntaId,
        string usuarioId,
        int opcionSeleccionadaIndex,
        bool esCorrecta,
        int assignedScore,
        TimeSpan tiempoEmpleado)
    {
        // domain validation
        var id = RespuestaTriviaId.New();
        var now = DateTimeOffset.UtcNow;
        return new RespuestaTrivia
        {
            Id = id,
            PartidaId = partidaId,
            PreguntaId = preguntaId,
            UsuarioId = usuarioId,
            OpcionSeleccionadaIndex = opcionSeleccionadaIndex,
            EsCorrecta = esCorrecta,
            PuntajeObtenido = esCorrecta ? assignedScore : 0,
            TiempoEmpleado = tiempoEmpleado,
            FechaRespuesta = now
        };
    }
}
```

### Extensiones a `PartidaTrivia`

```csharp
// Nuevas propiedades
public QuestionId? PreguntaActualId { get; private set; }
public bool PreguntaActiva => PreguntaActualId is not null;
private readonly List<RespuestaTrivia> _respuestas = new();
public IReadOnlyCollection<RespuestaTrivia> Respuestas => _respuestas.AsReadOnly();
// Nuevo campo para llevar el acumulado de puntaje por participante
private readonly Dictionary<string, int> _puntajesAcumulados = new();
public IReadOnlyDictionary<string, int> PuntajesAcumulados =>
    _puntajesAcumulados.AsReadOnly();

// Nuevos métodos

public void AbrirPrimeraPregunta()
{
    if (Estado != PartidaEstado.Iniciada)
        throw new InvalidStateTransitionException(Estado.ToString(), "Iniciada");
    if (PreguntaActiva)
        throw new InvalidOperationException("Ya hay una pregunta activa.");
    // La primera pregunta se obtiene del formulario y se activa
    PreguntaActualId = ObtenerPrimeraPreguntaId();
}

public (RespuestaTrivia respuesta, bool preguntaCerrada) RegistrarRespuestaDefinitiva(
    Question pregunta,
    string usuarioId,
    int opcionIndex,
    TimeSpan tiempoEmpleado)
{
    // 1. Validar estado
    if (Estado != PartidaEstado.Iniciada)
        throw new EstadoPartidaInvalidoException("Iniciada");
    if (PreguntaActualId is null || PreguntaActualId != pregunta.Id)
        throw new PreguntaNoActivaException();

    // 2. Validar no repetida
    if (_respuestas.Any(r => r.UsuarioId == usuarioId && r.PreguntaId == pregunta.Id))
        throw new RespuestaDuplicadaException(usuarioId, pregunta.Id.Value);

    // 3. Validar tiempo
    if (tiempoEmpleado.TotalSeconds > pregunta.TimeLimit.Seconds)
        throw new RespuestaTardiaException(pregunta.TimeLimit.Seconds);

    // 4. Validar modalidad individual
    if (Modalidad != Enums.Modalidad.Individual)
        throw new ModalidadInvalidaException("Individual");

    // 5. Evaluar opción seleccionada
    var esCorrecta = pregunta.GetCorrectOption().EqualsByIndex(opcionIndex);
    var respuesta = RespuestaTrivia.Create(
        Id, pregunta.Id, usuarioId, opcionIndex,
        esCorrecta, pregunta.AssignedScore.Value, tiempoEmpleado);

    _respuestas.Add(respuesta);

    // 6. Acumular puntaje si correcta
    if (esCorrecta)
    {
        if (!_puntajesAcumulados.ContainsKey(usuarioId))
            _puntajesAcumulados[usuarioId] = 0;
        _puntajesAcumulados[usuarioId] += pregunta.AssignedScore.Value;
    }

    // 7. Cerrar pregunta si acierta
    bool cerrarPregunta = esCorrecta;
    if (cerrarPregunta)
    {
        CerrarPreguntaActual();
    }

    AddDomainEvent(new RespuestaTriviaRegistradaDomainEvent(
        Id, pregunta.Id.Value, usuarioId, esCorrecta,
        respuesta.PuntajeObtenido, respuesta.TiempoEmpleado));

    return (respuesta, cerrarPregunta);
}

private void CerrarPreguntaActual()
{
    PreguntaActualId = null;
    // Avanzar a siguiente pregunta (si existe)
    // Esto se maneja en capa de aplicación o en HU-25/HU-28
}
```

### Value objects

| Type | Properties | Validation |
| --- | --- | --- |
| `RespuestaTriviaId` | `Guid Value` | Non-empty GUID |

### Domain events

| Event | Trigger | Notes |
| --- | --- | --- |
| `RespuestaTriviaRegistradaDomainEvent` | After answer is registered and validated | In-process; carries answer result for audit/real-time |

```csharp
public sealed record RespuestaTriviaRegistradaDomainEvent(
    PartidaId PartidaId,
    Guid PreguntaId,
    string UsuarioId,
    bool EsCorrecta,
    int PuntajeObtenido,
    TimeSpan TiempoEmpleado
) : DomainEvent;
```

### Domain exceptions

| Exception | When |
| --- | --- |
| `EstadoPartidaInvalidoException` | Partida not in Iniciada |
| `PreguntaNoActivaException` | No active question |
| `RespuestaDuplicadaException` | Same user already answered this question |
| `RespuestaTardiaException` | Time limit exceeded |
| `ModalidadInvalidaException` | Game is not Individual modality |

## Application layer (CQRS + MediatR)

### Command

```csharp
public sealed record AnswerTriviaQuestionCommand(
    Guid PartidaId,
    Guid PreguntaId,
    int OpcionSeleccionadaIndex
) : IRequest<AnswerTriviaResultDto>;
```

### Handler flow: `AnswerTriviaQuestionCommandHandler`

1. Validate command (FluentValidation).
2. Load `PartidaTrivia` aggregate via `IPartidaTriviaRepository` (include questions from form).
3. Validate participation: user is registered (`TriviaInscripcion` exists).
4. Load the question from the form (`TriviaForm` → `Questions.Single(q => q.Id == preguntaId)`).
5. Calculate `tiempoEmpleado` = (current UTC - `StartedAtUtc` of the question — requires storing question open timestamp).
6. Call `partida.RegistrarRespuestaDefinitiva(pregunta, usuarioId, opcionIndex, tiempoEmpleado)`.
7. Persist aggregate (EF Core saves new `RespuestaTrivia` and updated `PartidaTrivia`).
8. Publish domain events.
9. Send SignalR notification (question closed / answer recorded).
10. Return `AnswerTriviaResultDto`.

### Response DTO

```csharp
public sealed record AnswerTriviaResultDto(
    bool EsCorrecta,
    int PuntajeObtenido,
    bool PreguntaCerrada
);
```

### Validators (FluentValidation)

- `AnswerTriviaQuestionCommandValidator` — validates `PartidaId`, `PreguntaId` are not empty, `OpcionSeleccionadaIndex` is 0..3.

### Repository port (new)

```csharp
public interface IRespuestaTriviaRepository
{
    Task AddAsync(RespuestaTrivia respuesta, CancellationToken cancellationToken);
    Task<bool> ExisteRespuestaAsync(PartidaId partidaId, QuestionId preguntaId, string usuarioId, CancellationToken cancellationToken);
}
```

Modify `IPartidaTriviaRepository` to include question data loading and response tracking.

## Infrastructure layer

### EF Core entities

| Table | Key columns |
| --- | --- |
| `trivia_respuestas` | `id`, `partida_id`, `pregunta_id`, `usuario_id`, `opcion_seleccionada_index`, `es_correcta`, `puntaje_obtenido`, `tiempo_empleado_seconds`, `fecha_respuesta` |

New configuration class `RespuestaTriviaConfiguration`.

### Additions to `PartidaTrivia` persistence

- New column `pregunta_actual_id` (nullable GUID) on `trivia_partidas`.
- New owned collection `_respuestas` mapped to `trivia_respuestas`.

### Repository changes

- `PartidaTriviaRepository.LoadWithFormulasAsync()` or eager load `TriviaForm` + `Questions` when needed.
- `RespuestaTriviaRepository` for direct answer queries.

## API layer

### POST `/api/trivia-games/{partidaId}/questions/{preguntaId}/answer`

| Field | Value |
| --- | --- |
| Related HU | HU-26 |
| Related requirements | RF-20, RF-21, RF-22 |
| Authorization | Participante autenticado (rol Participante) |
| Type | Command |

**Request:**
```json
{
  "opcionSeleccionadaIndex": 2
}
```

**Response 200:**
```json
{
  "esCorrecta": true,
  "puntajeObtenido": 100,
  "preguntaCerrada": true
}
```

**Error responses:**

| Status | Reason |
| --- | --- |
| 400 | Invalid request data |
| 401 | Unauthenticated |
| 404 | Partida/pregunta not found, or user not registered |
| 409 | Duplicate answer, late answer, wrong state, wrong modality |

**Business rules:** RF-20, RF-21, RB-T21, RB-T24, RB-T25, RB-T28, RB-T29, TRIVIA-SCORE-001
**Events published:** `RespuestaTriviaRegistradaDomainEvent` (in-process)
**Real-time updates:** SignalR — notify participating clients about answer result and question close.

## Scoring and timer decision (explicit)

```txt
if respuesta.EsCorrecta:
    respuesta.PuntajeObtenido = pregunta.AssignedScore
else:
    respuesta.PuntajeObtenido = 0

# No se usa ningún factor de tiempo
# tiempoEmpleado se registra para telemetría e historial, no para puntaje
```

Rejected formula:
```txt
scoreEarned = assignedScore * (remainingTime / totalTime)
```

## Design patterns

| Pattern | Application |
| --- | --- |
| Aggregate Root | `PartidaTrivia` protects answer invariants |
| Entity | `RespuestaTrivia` as child of `PartidaTrivia` |
| CQRS | `AnswerTriviaQuestionCommand` + handler |
| Repository | `IPartidaTriviaRepository`, `IRespuestaTriviaRepository` |
| Domain Event | `RespuestaTriviaRegistradaDomainEvent` |
| Observer/PubSub | SignalR notification after answer recorded |

## Tests required

### Domain unit tests

- Accept correct answer → `EsCorrecta = true`, score = assignedScore.
- Accept incorrect answer → `EsCorrecta = false`, score = 0.
- Reject duplicate answer (same user, same question).
- Reject answer when no active question.
- Reject answer when game not Iniciada.
- Reject answer when time expired.
- Cerrar pregunta after correct answer.
- No cerrar pregunta after incorrect answer.

### Application handler tests

- Valid command → handler succeeds.
- Duplicate → 409.
- Late → 409.
- Not registered → 404.
- Wrong modality → 409.

### Integration tests

- POST answer → 200, verify persisted response.
- POST duplicate → 409.
- POST unauthorized → 401/403.

## Security

- JWT bearer authentication.
- Role policy `Participante` on the endpoint.
- User identity extracted from JWT `sub` claim.
