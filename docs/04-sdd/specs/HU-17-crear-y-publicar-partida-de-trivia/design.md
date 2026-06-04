# HU-17 — Design: Crear y publicar partida de Trivia

## Overview

| Aspecto | Decisión |
| --- | --- |
| Owning service | Trivia Game Service |
| Supporting services | Identity Service (autenticación JWT / rol Operador) |
| Client | React web |
| Architecture | Clean Architecture / Hexagonal |
| Application style | CQRS + MediatR |
| Persistence | PostgreSQL + EF Core |
| Real-time | No aplica directamente (el lobby se actualiza vía HU-21) |
| Async messaging | No aplica en esta HU |

## Bounded context

**Trivia Context** — subdominio de creación de partidas dentro de `Umbral.TriviaGame`.

## Domain model

### PartidaTrivia (agregado raíz)

El método `PartidaTrivia.Create()` valida todas las invariantes y devuelve una nueva instancia en estado `Lobby`.

```csharp
public static PartidaTrivia Create(
    NombrePartida nombre,
    Modalidad modalidad,
    ModoInicio modoInicio,
    TriviaFormId formularioId,
    OperatorId operatorId,
    TiempoInicio tiempoInicio,
    CantidadMinima minimo,
    CantidadMaximaJugadores? maxJugadores,
    CantidadMaximaEquipos? maxEquipos,
    JugadoresPorEquipoMin? minPorEquipo,
    JugadoresPorEquipoMax? maxPorEquipo)
```

### Value objects usados

| VO | Propósito |
| --- | --- |
| `NombrePartida` | Valida nombre (1-100 caracteres) |
| `CantidadMinima` | Valida mínimo ≥ 1 |
| `CantidadMaximaJugadores` | Valida máximo ≥ mínimo |
| `CantidadMaximaEquipos` | Valida máximo ≥ 1 |
| `JugadoresPorEquipoMin` | Valida mínimo ≥ 1 |
| `JugadoresPorEquipoMax` | Valida máximo ≥ mínimo |
| `TiempoInicio` | Valida fecha futura |
| `OperatorId` | Identificador del operador |

### Modalidad enum

```csharp
public enum Modalidad
{
    Individual = 0,
    Equipo = 1
}
```

### ModoInicio enum

```csharp
public enum ModoInicio
{
    Manual = 0,
    Automatico = 1,
    ManualYAutomatico = 2
}
```

## Application layer

### CreateTriviaGameCommand

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

### CreateTriviaGameCommandHandler

Flujo:

1. Obtener `FormularioTrivia` por Id.
2. Validar que exista — si no, lanzar `TriviaFormNotFoundException`.
3. Validar que `IsComplete` — si no, lanzar `FormularioIncompletoException`.
4. Crear value objects desde raw values.
5. Llamar `PartidaTrivia.Create(...)` con todos los VOs.
6. Persistir partida vía `IPartidaTriviaRepository.AddAsync`.
7. Despachar eventos de dominio.
8. Retornar `TriviaGameDetailDto`.

### Validators

`CreateTriviaGameCommandValidator` (FluentValidation):
- `Nombre`: not empty, 1-100 chars.
- `Modalidad`: must be "Individual" or "Equipo".
- `ModoInicio`: must be "Manual", "Automatico" or "ManualYAutomatico".
- `FormularioId`: not empty.
- `TiempoInicio`: must be future.
- `MinimoParticipantes`: ≥ 1.
- `MaximoJugadores`: required when Modalidad = Individual, ≥ MinimoParticipantes.
- `MaximoEquipos`: required when Modalidad = Equipo, ≥ 1.
- `MinimoJugadoresPorEquipo`: ≥ 1 when present.
- `MaximoJugadoresPorEquipo`: ≥ MinimoJugadoresPorEquipo when present.

## API layer

### POST /api/trivia-games

Ver contrato completo en `contracts/http/trivia-game-api.md`.

### TriviaGameMapper

Métodos estáticos para convertir entre dominio, comandos y DTOs:

- `ToModalidad(string value)`
- `ToModoInicio(string value)`
- `ToDto(PartidaTrivia partida)`

## Excepciones de dominio

| Excepción | HTTP Status |
| --- | --- |
| `TriviaFormNotFoundException` | 404 |
| `FormularioIncompletoException` | 400 |
| `DomainValidationException` (base) | 400 |

## Tests

### Handler tests

- `Handle_ValidIndividual_CreatesAndReturnsDto`
- `Handle_ValidEquipo_CreatesAndReturnsDto`
- `Handle_FormNotFound_ThrowsTriviaFormNotFoundException`
- `Handle_IncompleteForm_ThrowsFormularioIncompletoException`
- `Handle_InvalidModalidad_ThrowsMappingException`

### API tests

- `Create_ValidIndividual_Returns201`
- `Create_ValidEquipo_Returns201`
- `Create_FormNotFound_Returns404`
- `Create_NotOperador_Returns403`

## Design patterns

| Pattern | Aplicación |
| --- | --- |
| Aggregate Root | `PartidaTrivia.Create()` protege invariantes |
| Factory Method | `PartidaTrivia.Create()` encapsula construcción |
| Value Object | Validación encapsulada en `NombrePartida`, `CantidadMinima`, etc. |
| CQRS | `CreateTriviaGameCommand` / handler |
| Repository | `IPartidaTriviaRepository`, `ITriviaFormRepository` |
| Domain Event | `TriviaGameCreatedDomainEvent` |
| FluentValidation | Validación de entrada en capa Application |
