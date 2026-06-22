# HU-18 — Tasks

## Estado

Implementación completada. Todos los tests pasan (236 total, 12 nuevos de HU-18).

---

## Domain

| ID | Task | Status |
| --- | --- | --- |
| A-01 | Crear `TriviaInscripcionId` value object | Completed |
| A-02 | Crear `TriviaInscripcion` entity | Completed |

## Application

| ID | Task | Status |
| --- | --- | --- |
| A-03 | Crear `ITriviaInscripcionRepository` port | Completed |
| A-04 | Crear `JoinTriviaGameCommand` | Completed |
| A-05 | Crear `JoinTriviaGameCommandHandler` | Completed |

## Infrastructure

| ID | Task | Status |
| --- | --- | --- |
| I-01 | Agregar EF Core `DbSet<TriviaInscripcion>` y configuración | Completed |
| I-02 | Crear `TriviaInscripcionRepository` | Completed |
| I-03 | Crear `StubTriviaInscripcionRepository` | Completed |
| I-04 | Actualizar `PartidaTriviaRepository.CountInscripcionesAsync` | Completed |
| I-05 | Registrar en DI | Completed |
| I-06 | Crear migración EF Core | Completed |

## API

| ID | Task | Status |
| --- | --- | --- |
| P-01 | Agregar `POST /api/trivia-games/{gameId}/join` | Completed |

## Contracts

| ID | Task | Status |
| --- | --- | --- |
| C-01 | Documentar endpoint en `trivia-game-api.md` | Completed |

## Tests

| ID | Task | Status |
| --- | --- | --- |
| T-01 | Handler inscribe exitosamente | Completed |
| T-02 | Handler rechaza game no existe | Completed |
| T-03 | Handler rechaza game no lobby | Completed |
| T-04 | Handler rechaza modalidad equipo | Completed |
| T-05 | Handler rechaza cupo lleno | Completed |
| T-06 | Handler rechaza ya inscrito | Completed |
| T-07 | API endpoint retorna 200 | Completed |
| T-08 | API endpoint retorna 400/409 | Completed |

## Acceptance

| ID | Task | Status |
| --- | --- | --- |
| AT-01 | Completar `acceptance.md` | Completed |
| AT-02 | Actualizar `traceability-matrix.md` | Completed |
| AT-03 | Cerrar `tasks.md` y `SPECS-LIST.md` | Completed |
