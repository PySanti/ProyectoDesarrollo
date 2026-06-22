# HU-11 — Design: Filtrar partidas de Trivia por modalidad

## Overview

| Aspecto | Decisión |
| --- | --- |
| Owning service | Trivia Game Service |
| Supporting services | Identity Service (auth JWT) |
| Client | React Native mobile (congelado) |
| Architecture | Clean Architecture / Hexagonal |
| Application style | CQRS + MediatR (Query con filtro) |
| Persistence | PostgreSQL + EF Core |
| Real-time | No aplica |
| Async messaging | No aplica |

## Bounded context

**Trivia Context** — subdominio de consulta de partidas publicadas con filtro.

## Domain model

No se requieren cambios. HU-11 es solo lectura con filtrado.

## Application layer (CQRS + MediatR)

### Query modificada

```csharp
public sealed record GetPublishedTriviaGamesQuery(string? Modalidad)
    : IRequest<IReadOnlyList<TriviaGameListItemDto>>;
```

El parámetro `Modalidad` es opcional. Si es `null` o vacío, no se filtra.

### Handler modificado

```csharp
public async Task<IReadOnlyList<TriviaGameListItemDto>> Handle(
    GetPublishedTriviaGamesQuery request,
    CancellationToken cancellationToken)
{
    var partidas = await _repository.GetPublishedAsync(cancellationToken);

    var result = partidas
        .Select(TriviaGameMapper.ToListItemDto);

    if (!string.IsNullOrWhiteSpace(request.Modalidad))
    {
        var modalidad = TriviaGameMapper.ParseModalidad(request.Modalidad);
        result = result.Where(g => g.Modalidad == modalidad.ToString());
    }

    return result.ToList();
}
```

### Nuevo método en `TriviaGameMapper`

```csharp
public static Modalidad ParseModalidad(string value)
{
    return value.Trim().ToLowerInvariant() switch
    {
        "individual" => Modalidad.Individual,
        "equipo" => Modalidad.Equipo,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value,
            $"Modalidad inválida: {value}. Valores permitidos: Individual, Equipo.")
    };
}
```

Uso de `ParseModalidad` case-insensitive para query parameter.

## API layer

| Field | Valor |
| --- | --- |
| Method | GET |
| Path | `/api/trivia-games` |
| Query params | `?modalidad=Individual\|Equipo` (opcional) |
| MediatR | `GetPublishedTriviaGamesQuery` con `Modalidad` poblado |
| Auth | Cualquier autenticado |
| Success | 200 + lista |
| Error | 400 si modalidad inválida, 401 si no autenticado |

```csharp
[HttpGet]
public async Task<IActionResult> GetAll(
    [FromQuery] string? modalidad,
    CancellationToken cancellationToken)
{
    var query = new GetPublishedTriviaGamesQuery(modalidad);
    var result = await _mediator.Send(query, cancellationToken);
    return Ok(result);
}
```

## Design patterns

| Pattern | Application |
| --- | --- |
| CQRS | Query con filtro opcional |
| Repository | Sin cambios |
| Mapper | Nuevo método `ParseModalidad` case-insensitive |

## Tests required

### Application unit tests

- T-01: Filtro Individual retorna solo esas partidas.
- T-02: Filtro Equipo retorna solo esas partidas.
- T-03: Sin filtro retorna todas (backward compat).
- T-04: Filtro sin coincidencias retorna lista vacía.

### API integration tests

- T-05: `GET /api/trivia-games?modalidad=Individual` retorna 200 con filtro.

## Security

- JWT bearer authentication.
- Endpoint accesible por cualquier usuario autenticado.

## Dependencies

| Dependencia | Propósito |
| --- | --- |
| IPartidaTriviaRepository | Consultar partidas publicadas |
| TriviaGameMapper.ParseModalidad | Parseo case-insensitive |
