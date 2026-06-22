# HU-24 — Design: Iniciar manualmente Trivia

## Overview

| Aspecto | Decisión |
| --- | --- |
| Owning service | Trivia Game Service |
| Supporting services | Identity Service (autenticación JWT / rol Operador) |
| Client | React web (backend only, frontend frozen) |
| Architecture | Clean Architecture / Hexagonal |
| Application style | CQRS + MediatR |
| Persistence | PostgreSQL + EF Core |
| Real-time | SignalR `/hubs/trivia-lobby` — `GameStarted` event |
| Async messaging | No aplica en esta HU |

## Cambios respecto al estado actual

La HU-24 refina el comando `StartTriviaGame` ya existente. Los cambios son:

1. **Domain**: Agregar `ModoInicio.ManualYAutomatico` al enum.
2. **Domain**: Agregar validación en `PartidaTrivia.Iniciar()` para rechazar inicio manual cuando `ModoInicio == Automatico`.
3. **Domain**: Nueva excepción `ModoInicioAutomaticoException`.
4. **Application**: Modificar handler para pasar `esInicioManual: true`.
5. **Mapper**: Agregar "ManualYAutomatico" a `ToModoInicio()`.
6. **Tests**: Agregar tests handler y API para el nuevo escenario.

## Bounded context

**Trivia Context** — subdominio de ejecución de partidas dentro de `Umbral.TriviaGame`.

## Domain model

### ModoInicio enum (cambio)

```csharp
public enum ModoInicio
{
    Manual = 0,
    Automatico = 1,
    ManualYAutomatico = 2   // NEW
}
```

### PartidaTrivia.Iniciar (cambio)

```csharp
public void Iniciar(int cantidadInscriptos, bool esInicioManual = false)
{
    if (Estado != PartidaEstado.Lobby)
        throw new InvalidStateTransitionException(Estado.ToString(), "Iniciada");

    if (cantidadInscriptos < MinimoParticipantes.Value)
        throw new MinimosNoCumplidosException(cantidadInscriptos, MinimoParticipantes.Value);

    if (esInicioManual && ModoInicio == ModoInicio.Automatico)
        throw new ModoInicioAutomaticoException();

    Estado = PartidaEstado.Iniciada;
    StartedAtUtc = DateTimeOffset.UtcNow;

    AddDomainEvent(new PartidaTriviaIniciadaDomainEvent(Id, Nombre, StartedAtUtc.Value));
}
```

### Nueva excepción

```csharp
public sealed class ModoInicioAutomaticoException : DomainValidationException
{
    public ModoInicioAutomaticoException()
        : base("No se puede iniciar manualmente una partida configurada solo con inicio automático.") { }
}
```

## Application layer (cambios)

### StartTriviaGameCommandHandler (cambio)

Pasar `esInicioManual: true` a `partida.Iniciar()`:

```csharp
partida.Iniciar(cantidadInscriptos, esInicioManual: true);
```

### TriviaGameMapper.ToModoInicio (cambio)

```csharp
public static ModoInicio ToModoInicio(string value)
{
    return value switch
    {
        "Manual" => ModoInicio.Manual,
        "Automatico" => ModoInicio.Automatico,
        "ManualYAutomatico" => ModoInicio.ManualYAutomatico,
        _ => throw new ArgumentOutOfRangeException(...)
    };
}
```

## API layer

Sin cambios. El endpoint `POST /api/trivia-games/{id}/start` ya existe y usa el handler modificado.

### Nuevo error mapping

| Excepción | HTTP Status |
| --- | --- |
| `ModoInicioAutomaticoException` | 409 Conflict |

## Tests

### Handler tests (nuevos)

- `Handle_ModoInicioAutomatico_ThrowsModoInicioAutomaticoException`
- `Handle_ManualYAutomatico_AllowsStart`

### API tests (nuevos)

- `Start_ModoInicioAutomatico_Returns409`

## Design patterns

| Pattern | Aplicación |
| --- | --- |
| Aggregate Root | `PartidaTrivia` protege transición de estado |
| Domain Exception | `ModoInicioAutomaticoException` para invariante |
| CQRS | `StartTriviaGameCommand` / handler |
| Repository | `IPartidaTriviaRepository` |
| Domain Event | `PartidaTriviaIniciadaDomainEvent` |
