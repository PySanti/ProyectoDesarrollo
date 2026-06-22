# HU-09 — Design: Ver partidas de Trivia publicadas

## Overview

| Aspecto | Decisión |
| --- | --- |
| Owning service | Trivia Game Service |
| Supporting services | Identity Service (auth JWT) |
| Client | React Native mobile (congelado) |
| Architecture | Clean Architecture / Hexagonal |
| Application style | CQRS + MediatR (Query) |
| Persistence | PostgreSQL + EF Core |
| Real-time | No aplica |
| Async messaging | No aplica |

## Bounded context

**Trivia Context** — subdominio de consulta de partidas publicadas.

## Domain model

No se requieren cambios. HU-09 es solo lectura y no modifica el agregado `PartidaTrivia`.

## Application layer (CQRS + MediatR)

### DTO

```csharp
public sealed record TriviaGameListItemDto(
    Guid Id,
    string Nombre,
    string Modalidad,
    string Estado,
    DateTimeOffset TiempoInicio,
    int MinimoParticipantes,
    int? MaximoJugadores,
    int? MaximoEquipos);
```

### Query

```csharp
public sealed record GetPublishedTriviaGamesQuery
    : IRequest<IReadOnlyList<TriviaGameListItemDto>>;
```

### Handler

```csharp
public sealed class GetPublishedTriviaGamesQueryHandler
    : IRequestHandler<GetPublishedTriviaGamesQuery, IReadOnlyList<TriviaGameListItemDto>>
```

1. Llama `_repository.GetPublishedAsync()`.
2. Mapea cada `PartidaTrivia` a `TriviaGameListItemDto`.
3. Retorna lista.

### Repository port — nuevo método

```csharp
Task<IReadOnlyList<PartidaTrivia>> GetPublishedAsync(CancellationToken cancellationToken = default);
```

Filtro: `p => p.Estado == PartidaEstado.Lobby`.

## Infrastructure layer

### Repositorio EF Core

```csharp
public async Task<IReadOnlyList<PartidaTrivia>> GetPublishedAsync(CancellationToken ct)
{
    return await _dbContext.PartidasTrivia
        .Where(p => p.Estado == PartidaEstado.Lobby)
        .ToListAsync(ct);
}
```

## API layer

| Method | Path | MediatR | Auth | Success | Errors |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/trivia-games` | `GetPublishedTriviaGamesQuery` | Autenticado (Participante u Operador) | 200 + lista | 401 |

### Auth

El controlador existente `TriviaGamesController` tiene `[Authorize(Policy = PolicyNames.Operador)]` a nivel de clase. Para HU-09:

- Opción elegida: crear un segundo controller `TriviaGamesPublicController` sin policy específica, solo `[Authorize]` (cualquier usuario autenticado).

Esto evita conflictos de policy OR y mantiene la seguridad existente en los endpoints de operador.

### Endpoint

```csharp
[ApiController]
[Authorize]
[Route("api/trivia-games")]
public sealed class TriviaGamesPublicController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) { ... }
}
```

## Design patterns

| Pattern | Application |
| --- | --- |
| CQRS | Query separada de comandos |
| Repository | `IPartidaTriviaRepository.GetPublishedAsync()` |

## Tests required

### Application unit tests

- Handler retorna solo partidas con Estado == Lobby.
- Handler retorna lista vacía si no hay partidas publicadas.

### API integration tests

- GET retorna 200 con lista (autenticado Participante).
- GET retorna lista vacía si no hay partidas.
- GET retorna 401 si no autenticado.

## Security

- JWT bearer authentication.
- Endpoint accesible por cualquier usuario autenticado (Participante u Operador).

## Dependencies

| Dependencia | Propósito |
| --- | --- |
| IPartidaTriviaRepository | Consultar partidas |
| TriviaGameMapper (extensión ToListItemDto) | Mapeo a DTO liviano |
