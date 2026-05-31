# HU-09 — Tasks: Ver partidas de Trivia publicadas

Implementar **una tarea a la vez**. No iniciar implementación hasta aprobación del SDD.

## Convenciones

- Servicio: `services/trivia-game-service/`
- Solución .NET 8 con capas: `Domain`, `Application`, `Infrastructure`, `Api`
- Código de producción y pruebas en **inglés**
- Cliente: React Native mobile (congelado; no implementar en este sprint)
- Sin cambios en Domain Layer (solo lectura)

---

## 1. Application

| ID | Task | Definition of done |
| --- | --- | --- |
| A-04 | Crear DTO `TriviaGameListItemDto` con Id, Nombre, Modalidad, Estado, TiempoInicio, MinimoParticipantes, MaximoJugadores?, MaximoEquipos? | ✅ Creado en `Application/Dtos/` |
| A-01 | Agregar `Task<IReadOnlyList<PartidaTrivia>> GetPublishedAsync(CancellationToken)` al puerto `IPartidaTriviaRepository` | ✅ Interface extendida |
| A-02 | Crear Query `GetPublishedTriviaGamesQuery : IRequest<IReadOnlyList<TriviaGameListItemDto>>` | ✅ Query record creado |
| A-03 | Crear Handler `GetPublishedTriviaGamesQueryHandler` que llama repositorio y mapea | ✅ Handler implementado |
| A-05 | Agregar método `ToListItemDto` al mapper `TriviaGameMapper` | ✅ Mapper extendido |

## 2. Infrastructure

| ID | Task | Definition of done |
| --- | --- | --- |
| I-01 | Implementar `GetPublishedAsync()` en `PartidaTriviaRepository` (filtro Estado == Lobby) | ✅ Repositorio actualizado |

## 3. API

| ID | Task | Definition of done |
| --- | --- | --- |
| P-01 | Crear controller `TriviaGamesPublicController` con `[Authorize]` (cualquier autenticado) y endpoint `GET /api/trivia-games` | ✅ Endpoint funcional |
| P-02 | Documentar endpoint en `contracts/http/trivia-game-api.md` (sección HU-09) | ✅ Contrato actualizado |

## 4. Tests

| ID | Task | Definition of done |
| --- | --- | --- |
| T-01 | Test unitario Application: handler retorna solo partidas con Estado Lobby | ✅ |
| T-02 | Test unitario Application: handler retorna lista vacía si no hay partidas | ✅ |
| T-03 | Test integración API: `GET /api/trivia-games` retorna 200 con lista (autenticado Participante) | ✅ |
| T-04 | Test integración API: `GET /api/trivia-games` retorna 200 con lista (autenticado Operador) y lista vacía sin partidas | ✅ |

## 5. Acceptance

| ID | Task | Definition of done |
| --- | --- | --- |
| AT-01 | Completar `acceptance.md` con checklist verificado | ✅ |
| AT-02 | Actualizar `docs/04-sdd/traceability-matrix.md` fila HU-09 | ✅ |
| AT-03 | Marcar tareas completadas en este archivo | ✅ |

---

## Orden de implementación recomendado

```txt
A-04 → A-01 → A-02 → A-03 → A-05 → I-01
  → P-01 → P-02
  → T-01 → T-02 → T-03 → T-04
  → AT-01 → AT-02 → AT-03
```

## Estimación orientativa

| Capa | Esfuerzo relativo |
| --- | --- |
| Application + Infrastructure | 0.5 d |
| API | 0.25 d |
| Tests | 0.25 d |
| Acceptance | 0.25 d |
| **Total** | **~1.25 d** |
