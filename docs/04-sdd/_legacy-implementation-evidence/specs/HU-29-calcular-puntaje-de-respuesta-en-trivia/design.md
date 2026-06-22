# HU-29 — Design

## Owning service

Trivia Game Service

## Context

Trivia Context

## Aggregates touched

- `PartidaTrivia` (aggregate root) — ya tiene `ObtenerPuntajeAcumulado` y `ObtenerTiempoRespuestaAcumulado`
- `RespuestaTrivia` (entity hija) — ya tiene `PuntajeObtenido`, `EsCorrecta`, `TiempoEmpleadoSegundos`

## Cambios

No se modifica código de dominio existente. El scoring ya está implementado en `PartidaTrivia.RegistrarRespuestaDefinitiva` y `ObtenerPuntajeAcumulado`.

Lo que se agrega:

### Nuevo DTO

```csharp
public sealed record AccumulatedScoreDto(
    Guid PartidaId,
    int PuntajeAcumulado,
    double TiempoAcumuladoSegundos,
    int RespuestasCorrectas,
    int TotalRespuestas
);
```

### Nueva Query

```csharp
public sealed record GetAccumulatedScoreQuery(
    Guid PartidaId,
    string UsuarioId
) : IRequest<AccumulatedScoreDto>;
```

### Handler logic

1. Cargar `PartidaTrivia` con respuestas por `PartidaId`.
2. Si no existe, throw NotFound.
3. Calcular usando métodos del agregado:
   - `ObtenerPuntajeAcumulado(usuarioId)`
   - `ObtenerTiempoRespuestaAcumulado(usuarioId)`
   - Total de respuestas del participante
   - Respuestas correctas del participante
4. Retornar DTO.

### Endpoint

```
GET /api/trivia-games/{id}/score
Authorization: Bearer <token>
```

### Nuevos tests de dominio

Se agregan tests que verifican explícitamente:

| Test | Verifica |
|---|---|
| `Registrar_RespuestaCorrecta_AsignaExactamenteAssignedScore` | score = 100, assignedScore = 100 |
| `Registrar_RespuestaIncorrecta_SinImportarTiempo_PuntajeEsCero` | incorrecta con elapsed alto sigue siendo 0 |
| `ObtenerPuntajeAcumulado_ConVariosAssignedScores_SumaCorrectamente` | suma de 50+200 = 250 |

## Design Patterns Applied

| Pattern | Location | Problem solved |
|---|---|---|
| CQRS | `GetAccumulatedScoreQuery` | Lectura separada de escritura |
| Mediator | MediatR handler pipeline | Orquestación del caso de uso |
| Aggregate Method | `PartidaTrivia.ObtenerPuntajeAcumulado()` | Encapsular lógica de suma dentro del agregado |
