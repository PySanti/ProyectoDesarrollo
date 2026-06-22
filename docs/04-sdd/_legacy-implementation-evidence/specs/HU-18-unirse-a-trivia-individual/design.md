# HU-18 — Design

## Contexto acotado

Trivia Context (`Umbral.TriviaGame.Domain`).

## Agregados

### TriviaInscripcion (nuevo)

| Elemento | Tipo |
| --- | --- |
| `TriviaInscripcion` | Entidad |
| `TriviaInscripcionId` | Value Object |
| `PartidaId` | Value Object (existente) |
| `UsuarioId` | string (id del claim "sub") |
| `FechaInscripcion` | DateTimeOffset |

`TriviaInscripcion` es un agregado separado de `PartidaTrivia`.

### Impacto en PartidaTrivia (existente)

`PartidaTrivia` NO cambia su estructura. Las validaciones de capacidad usan el repositorio de inscripciones.

## Servicio dueño

Trivia Game Service.

## Puertos (Application)

### ITriviaInscripcionRepository (nuevo)

```csharp
Task<int> CountByPartidaIdAsync(PartidaId partidaId, CancellationToken ct = default);
Task AddAsync(TriviaInscripcion inscripcion, CancellationToken ct = default);
```

## Commands

### JoinTriviaGameCommand

| Propiedad | Tipo |
| --- | --- |
| `PartidaId` | PartidaId |
| `UsuarioId` | string |

Handler valida:
1. Game existe (`IPartidaTriviaRepository.GetByIdAsync`).
2. Game está en estado Lobby.
3. Game es modalidad Individual.
4. Cupo disponible (`CountByPartidaIdAsync < MaximoJugadores`).
5. Usuario no duplicado (no existe inscripción con mismo `PartidaId` + `UsuarioId`).
6. Crea `TriviaInscripcion` y persiste.

## Eventos publicados

Ninguno para esta HU.

## Actualizaciones en tiempo real

Ninguna para esta HU.

## Impacto en StartTriviaGameCommandHandler

Actualizar para inyectar `ITriviaInscripcionRepository` y llamar `CountByPartidaIdAsync` en lugar de `IPartidaTriviaRepository.CountInscripcionesAsync`.

## Patrones de diseño aplicados

| Patrón | Ubicación | Problema resuelto | Justificación |
| --- | --- | --- | --- |
| CQRS | Command/Handler | Separar escritura (join) de consultas | Arquitectura obligatoria |
| Mediator | MediatR | Orquestar caso de uso | Arquitectura obligatoria |
| Repository | Interfaces | Abstraer persistencia | Arquitectura obligatoria |
| Adapter | EF Core repo | Aislar infraestructura | Arquitectura obligatoria |
| Domain Entity | TriviaInscripcion | Mantener invariantes de inscripción | DDD táctico |

No se introducen patrones tácticos adicionales.

## Pruebas planificadas

### Handler tests (Aplicación)

1. Handler inscribe exitosamente con cupo.
2. Handler rechaza si game no existe (null).
3. Handler rechaza si game no está en Lobby.
4. Handler rechaza si game es modalidad Equipo.
5. Handler rechaza si cupo lleno.
6. Handler rechaza si usuario ya inscrito.

### API tests (Integración)

7. POST retorna 200 OK con inscripción exitosa.
8. POST retorna 400/409 para validaciones fallidas.
