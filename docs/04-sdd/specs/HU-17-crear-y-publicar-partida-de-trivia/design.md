# HU-17 — Design: Crear y publicar partida de Trivia

## Overview

| Aspecto | Decisión |
| --- | --- |
| Owning service | Trivia Game Service |
| Supporting services | Identity Service (auth JWT / rol Operador) |
| Client | React web (panel operador) |
| Architecture | Clean Architecture / Hexagonal |
| Application style | CQRS + MediatR |
| Persistence | PostgreSQL + EF Core |
| Real-time | No aplica en esta HU |
| Async messaging | No aplica en esta HU |

## Bounded context

**Trivia Context** — subdominio de ejecución de partidas (`PartidaTrivia` aggregate) dentro del mismo microservicio que `FormularioTrivia`.

## Domain model (DDD — C# .NET 8)

Nombres de clases, propiedades y métodos en **inglés**. Entidades con **setters privados**; mutaciones vía métodos de dominio.

### Enums

```csharp
public enum PartidaEstado
{
    Lobby,
    Iniciada,
    Cancelada,
    Terminada
}

public enum Modalidad
{
    Individual,
    Equipo
}

public enum ModoInicio
{
    Manual,
    Automatico
}
```

### Value Objects

| Type | Properties | Validation |
| --- | --- | --- |
| `PartidaId` | `Guid Value` | Non-empty GUID |
| `NombrePartida` | `string Value` | Not null/whitespace; 3..100 chars |
| `TiempoInicio` | `DateTimeOffset Value` | Must be in the future at creation |
| `CantidadMinima` | `int Value` | >= 1 |
| `CantidadMaximaJugadores` | `int Value` | >= CantidadMinima |
| `CantidadMaximaEquipos` | `int Value` | >= 1 |
| `JugadoresPorEquipoMin` | `int Value` | >= 1 |
| `JugadoresPorEquipoMax` | `int Value` | >= JugadoresPorEquipoMin |

### Aggregate root: `PartidaTrivia`

```csharp
public sealed class PartidaTrivia : AggregateRoot<PartidaId>
{
    public NombrePartida Nombre { get; private set; }
    public PartidaEstado Estado { get; private set; }
    public Modalidad Modalidad { get; private set; }
    public ModoInicio ModoInicio { get; private set; }
    public FormularioId FormularioAsociadoId { get; private set; }
    public OperatorId CreatedByOperatorId { get; private set; }
    public TiempoInicio TiempoInicio { get; private set; }
    public CantidadMinima MinimoParticipantes { get; private set; }

    // Modalidad Individual
    public CantidadMaximaJugadores? MaximoJugadores { get; private set; }

    // Modalidad Equipo
    public CantidadMaximaEquipos? MaximoEquipos { get; private set; }
    public JugadoresPorEquipoMin? MinimoJugadoresPorEquipo { get; private set; }
    public JugadoresPorEquipoMax? MaximoJugadoresPorEquipo { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }

    private PartidaTrivia() { } // EF Core materialization

    public static PartidaTrivia Create(
        NombrePartida nombre,
        Modalidad modalidad,
        ModoInicio modoInicio,
        FormularioId formularioId,
        OperatorId operatorId,
        TiempoInicio tiempoInicio,
        CantidadMinima minimoParticipantes,
        CantidadMaximaJugadores? maximoJugadores,
        CantidadMaximaEquipos? maximoEquipos,
        JugadoresPorEquipoMin? minJugadoresPorEquipo,
        JugadoresPorEquipoMax? maxJugadoresPorEquipo);

    public void PublicarLobby();
    public void Iniciar();
    public void Cancelar();
    public bool PuedeIniciar(int cantidadInscriptos);
}
```

**Invariants (aggregate level):**

- `Nombre` must be non-empty (enforced by VO).
- `FormularioAsociadoId` must reference a form with `IsComplete = true`.
- If `Modalidad == Individual`, `MaximoJugadores` must be set, `MaximoEquipos` must be null.
- If `Modalidad == Equipo`, `MaximoEquipos`, `MinimoJugadoresPorEquipo`, `MaximoJugadoresPorEquipo` must be set, `MaximoJugadores` must be null.
- State transitions: Lobby -> Iniciada, Lobby -> Cancelada, Iniciada -> Cancelada, Iniciada -> Terminada.
- `Iniciar()` requires `PuedeIniciar(cantidadInscriptos)` to be true.
- `Cancelar()` allowed only in Lobby or Iniciada.

### Domain exceptions

| Exception | When |
| --- | --- |
| `PartidaTriviaNotFoundException` | Lookup miss (mapped to 404) |
| `InvalidStateTransitionException` | Illegal state change |
| `MinimosNoCumplidosException` | Start rejected due to unmet minimums |
| `FormularioIncompletoException` | Associated form is not complete |
| `ModalidadInvalidaException` | Modalidad/limites mismatch |

### Domain events (in-process)

| Event | Trigger |
| --- | --- |
| `PartidaTriviaCreadaDomainEvent` | After successful create |
| `PartidaTriviaPublicadaDomainEvent` | After lobby publication |
| `PartidaTriviaIniciadaDomainEvent` | After state changes to Iniciada |
| `PartidaTriviaCanceladaDomainEvent` | After state changes to Cancelada |

## Application layer (CQRS + MediatR)

### Commands

#### `CreateTriviaGameCommand`

```csharp
public sealed record CreateTriviaGameCommand(
    string Nombre,
    string Modalidad,
    string ModoInicio,
    Guid FormularioId,
    DateTimeOffset TiempoInicio,
    int MinimoParticipantes,
    int? MaximoJugadores,
    int? MaximoEquipos,
    int? MinimoJugadoresPorEquipo,
    int? MaximoJugadoresPorEquipo
) : IRequest<TriviaGameDetailDto>;
```

**Handler flow:**
1. Validate command (FluentValidation).
2. Load `TriviaForm` by id; 404 if not found; 400 if not complete.
3. Map to value objects.
4. `PartidaTrivia.Create(...)`.
5. Persist via `IPartidaTriviaRepository`.
6. Return `TriviaGameDetailDto`.

#### `StartTriviaGameCommand`

```csharp
public sealed record StartTriviaGameCommand(Guid PartidaId) : IRequest<TriviaGameDetailDto>;
```

**Handler flow:**
1. Load `PartidaTrivia` by id; 404 if not found.
2. Count current inscriptions (consult via repository or query service).
3. `partida.Iniciar()` — throws `MinimosNoCumplidosException` if not met, or `InvalidStateTransitionException` if not in Lobby.
4. Persist changes.
5. Return updated `TriviaGameDetailDto`.

### Queries

#### `GetTriviaGameByIdQuery`

```csharp
public sealed record GetTriviaGameByIdQuery(Guid PartidaId) : IRequest<TriviaGameDetailDto?>;
```

### Response DTO: `TriviaGameDetailDto`

```csharp
public sealed record TriviaGameDetailDto(
    Guid Id,
    string Nombre,
    string Estado,
    string Modalidad,
    string ModoInicio,
    Guid FormularioId,
    DateTimeOffset TiempoInicio,
    int MinimoParticipantes,
    int? MaximoJugadores,
    int? MaximoEquipos,
    int? MinimoJugadoresPorEquipo,
    int? MaximoJugadoresPorEquipo,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? StartedAtUtc
);
```

### Validators (FluentValidation)

- `CreateTriviaGameCommandValidator` — mirrors domain rules about modality/limits consistency.
- `StartTriviaGameCommandValidator` — validates PartidaId is not empty.

### Repository port

```csharp
public interface IPartidaTriviaRepository
{
    Task AddAsync(PartidaTrivia partida, CancellationToken ct);
    Task<PartidaTrivia?> GetByIdAsync(PartidaId id, CancellationToken ct);
    Task UpdateAsync(PartidaTrivia partida, CancellationToken ct);
}
```

## Infrastructure layer

### EF Core mapping

| Table | Key columns |
| --- | --- |
| `partidas_trivia` | `id`, `nombre`, `estado`, `modalidad`, `modo_inicio`, `formulario_id`, `operador_id`, `tiempo_inicio`, `minimo_participantes`, `maximo_jugadores`, `maximo_equipos`, `min_jugadores_equipo`, `max_jugadores_equipo`, `created_at_utc`, `started_at_utc` |

- `PartidaTriviaConfiguration` maps all VOs via `HasConversion`.
- `FormularioId` stored as simple Guid column (referential integrity optional).
- New migration adds table to existing TriviaGameDbContext.

### Reuse from HU-15

- Same `TriviaGameDbContext`.
- Same `ValueConverters` static class (add new converters for new VOs).
- Same `DomainEventDispatcher` no-op implementation.
- Same DI registration pattern.

## API layer

Base path: `/api/trivia-games`  
Authorization policy: `RequireRole("Operador")` — reuse `PolicyNames.Operador`.

| Method | Path | MediatR | Success | Errors |
| --- | --- | --- | --- | --- |
| POST | `/api/trivia-games` | `CreateTriviaGameCommand` | 201 + body | 400, 401, 403, 404 |
| POST | `/api/trivia-games/{id}/start` | `StartTriviaGameCommand` | 200 + body | 400, 401, 403, 404, 409 |
| GET | `/api/trivia-games/{id}` | `GetTriviaGameByIdQuery` | 200 + body | 401, 403, 404 |

`TriviaGamesController` delegates to `IMediator`; no business logic in controller.

## HTTP contracts (to document in `contracts/http/trivia-game-api.md`)

### POST `/api/trivia-games`

**Related HU:** HU-17  
**Related requirement:** RF-17, RF-18  
**Authorization:** Operador

**Request:**
```json
{
  "nombre": "Trivia Demo Sprint 1",
  "modalidad": "Individual",
  "modoInicio": "Manual",
  "formularioId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "tiempoInicio": "2026-06-01T15:00:00Z",
  "minimoParticipantes": 2,
  "maximoJugadores": 10,
  "maximoEquipos": null,
  "minimoJugadoresPorEquipo": null,
  "maximoJugadoresPorEquipo": null
}
```

**Response 201:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "nombre": "Trivia Demo Sprint 1",
  "estado": "Lobby",
  "modalidad": "Individual",
  "modoInicio": "Manual",
  "formularioId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "tiempoInicio": "2026-06-01T15:00:00Z",
  "minimoParticipantes": 2,
  "maximoJugadores": 10,
  "maximoEquipos": null,
  "minimoJugadoresPorEquipo": null,
  "maximoJugadoresPorEquipo": null,
  "createdAtUtc": "2026-05-31T12:00:00Z",
  "startedAtUtc": null
}
```

**Error responses:**

| Status | Reason |
| --- | --- |
| 400 | Validation error / formulario incompleto / modalidad-limites mismatch |
| 401 | Unauthenticated |
| 403 | Not Operador |
| 404 | FormularioId not found |

### POST `/api/trivia-games/{id}/start`

**Related HU:** HU-17  
**Related requirement:** RF-18, RB-26, RB-27  
**Authorization:** Operador

**Request:** empty body

**Response 200:** same as `TriviaGameDetailDto` with `estado: "Iniciada"` and `startedAtUtc` set.

**Error responses:**

| Status | Reason |
| --- | --- |
| 400 | Invalid state transition |
| 401 | Unauthenticated |
| 403 | Not Operador |
| 404 | Partida not found |
| 409 | Minimos de participación no cumplidos |

### GET `/api/trivia-games/{id}`

**Related requirement:** RF-35  
**Authorization:** Operador

**Response 200:** `TriviaGameDetailDto`.  
**Response 404:** Not found.

## Design patterns

| Pattern | Application |
| --- | --- |
| Aggregate Root | `PartidaTrivia` protects state transitions and invariants |
| Value Object | `NombrePartida`, `TiempoInicio`, límites de participación |
| Factory method | `PartidaTrivia.Create(...)` |
| State | `PartidaEstado` enum — explicit transitions validated in domain |
| CQRS | Separate commands and queries |
| Repository | `IPartidaTriviaRepository` |

## Tests required

### Domain unit tests

- Create partida with valid data → estado Lobby.
- Reject creation with incomplete formulario.
- Reject creation with modalidad/limites mismatch.
- Start with sufficient inscriptions → estado Iniciada.
- Reject start without sufficient inscriptions.
- Reject state transitions that violate rules (e.g., Terminada -> Iniciada).
- Cancel in Lobby or Iniciada allowed.
- Cancel in Terminada rejected.

### Application unit tests

- Validators reject malformed commands.
- Create handler returns DTO with correct state.
- Start handler rejects minimums not met.

### Integration tests

- POST create persists round-trip GET.
- POST start changes state to Iniciada.
- POST start without inscriptions returns 409.
- GET returns 404 for unknown id.
- Authorization 403 for non-operator token.

## Security

- JWT bearer authentication.
- Role policy `Operador` on all endpoints.
- No participant or administrator access in this HU.

## Dependencies

| Dependency | Purpose |
| --- | --- |
| `FormularioTrivia` (HU-15) | Validate form existence and `IsComplete` at game creation |
| `ITriviaFormRepository` | Load form by id during create |
