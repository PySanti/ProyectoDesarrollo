# HU-21 — Tasks

## Estado

Completado.

## Application

| ID | Task | Status |
| --- | --- | --- |
| Q-01 | Crear `TriviaGameLobbyDto` | Done |
| Q-02 | Crear `GetTriviaGameLobbyQuery` | Done |
| Q-03 | Crear `GetTriviaGameLobbyQueryHandler` | Done |
| P-01 | Agregar `ListByPartidaIdAsync` a `ITriviaInscripcionRepository` port | Done |
| P-02 | Crear `ITriviaLobbyNotifier` port | Done |

## Infrastructure / API

| ID | Task | Status |
| --- | --- | --- |
| I-01 | Implementar `ListByPartidaIdAsync` en `TriviaInscripcionRepository` (real) | Done |
| I-02 | Implementar `ListByPartidaIdAsync` en `StubTriviaInscripcionRepository` | Done |
| I-03 | Crear `TriviaLobbyHub` + `TriviaLobbyNotifier` (SignalR adapter en Api) | Done |
| I-04 | Crear `StubTriviaLobbyNotifier` (para tests) | Done |
| I-05 | Registrar `ITriviaLobbyNotifier` en DI + `MapHub` en Program.cs | Done |

## API

| ID | Task | Status |
| --- | --- | --- |
| P-03 | Endpoint `GET /api/trivia-games/{id}/lobby` | Done |
| P-04 | Agregar `UsuarioNoInscritoException` al middleware (403) | Done |

## Handlers existentes (actualizar)

| ID | Task | Status |
| --- | --- | --- |
| S-01 | Inyectar `ITriviaLobbyNotifier` en `JoinTriviaGameCommandHandler` y notificar | Done |
| S-02 | Inyectar `ITriviaLobbyNotifier` en `StartTriviaGameCommandHandler` y notificar | Done |

## Contracts

| ID | Task | Status |
| --- | --- | --- |
| C-01 | Documentar endpoint y eventos SignalR en contratos | Done |

## Tests

| ID | Task | Status |
| --- | --- | --- |
| T-01 | Handler lobby: partida existe y usuario inscrito retorna DTO | Done |
| T-02 | Handler lobby: partida no existe lanza NotFound | Done |
| T-03 | Handler lobby: usuario no inscrito lanza NoAutorizado | Done |
| T-04 | API: lobby exitoso retorna 200 | Done |
| T-05 | API: game no existe retorna 404 | Done |
| T-06 | API: usuario no inscrito retorna 403 | Done |

## Acceptance

| ID | Task | Status |
| --- | --- | --- |
| AT-01 | Completar `acceptance.md` | Done |
| AT-02 | Actualizar `traceability-matrix.md` | Done |
| AT-03 | Cerrar `tasks.md` y `SPECS-LIST.md` | Done |
