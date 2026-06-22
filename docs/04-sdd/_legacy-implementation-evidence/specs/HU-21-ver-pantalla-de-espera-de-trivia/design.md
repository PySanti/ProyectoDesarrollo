# HU-21 — Design

## Contexto acotado

Trivia Context (Trivia Game Service).

## Agregados involucrados

- `PartidaTrivia` — estado, nombre, modalidad, tiempo de inicio, máximos (solo lectura).
- `TriviaInscripcion` — lista de participantes inscritos (solo lectura).

## Puertos nuevos

| Puerto | Métodos | Capa |
| --- | --- | --- |
| `ITriviaInscripcionRepository` | + `ListByPartidaIdAsync(PartidaId)` | Application |
| `ITriviaLobbyNotifier` | `NotifyParticipantJoined()`, `NotifyGameStarted()`, `NotifyGameCancelled()` | Application |

## Comandos / Queries

| ID | Tipo | Descripción |
| --- | --- | --- |
| `GetTriviaGameLobbyQuery` | Query | Obtiene datos del lobby para un participante inscrito. |
| `TriviaGameLobbyDto` | DTO | Datos devueltos: nombre, estado, modalidad, tiempoInicio, participantes, maximoJugadores, minimoParticipantes. |

## Handler

`GetTriviaGameLobbyQueryHandler`:
1. Valida que `PartidaTrivia` exista (repo).
2. Valida que el `UsuarioId` esté inscrito en la partida (inscripcionRepo).
3. Obtiene lista de inscripciones (inscripcionRepo).
4. Retorna `TriviaGameLobbyDto`.

## SignalR

| Elemento | Descripción |
| --- | --- |
| Hub | `TriviaLobbyHub` en Api, ruta `/hubs/trivia-lobby` |
| Puerto | `ITriviaLobbyNotifier` en Application |
| Adaptador | `TriviaLobbyNotifier` en Infrastructure usando `IHubContext<TriviaLobbyHub>` |
| Eventos | `ParticipantJoined`, `GameStarted`, `GameCancelled` |

Los handlers `JoinTriviaGameCommandHandler` y `StartTriviaGameCommandHandler` reciben `ITriviaLobbyNotifier` y publican después de ejecutar la operación principal.

## Diseño de layers

### Application

```
Queries/
  GetTriviaGameLobbyQuery.cs
  TriviaGameLobbyDto.cs
Handlers/
  GetTriviaGameLobbyQueryHandler.cs
Ports/
  ITriviaLobbyNotifier.cs
```

### Infrastructure

```
Data/Repositories/
  TriviaInscripcionRepository.cs  (+ ListByPartidaIdAsync)
  StubTriviaInscripcionRepository.cs  (+ ListByPartidaIdAsync)
SignalR/
  TriviaLobbyNotifier.cs
  StubTriviaLobbyNotifier.cs
```

### Api

```
Hubs/
  TriviaLobbyHub.cs
Controllers/
  TriviaGamesPublicController.cs  (+ GET lobby endpoint)
Program.cs  (+ MapHub)
```

## Design Patterns Applied

| Pattern | Location | Problem solved | Justification |
| --- | --- | --- | --- |
| Adapter | `ITriviaLobbyNotifier` / `TriviaLobbyNotifier` | Aislar SignalR del dominio/aplicación | Infrastructure depende de SignalR; Application solo conoce el puerto. |
| Query object | `GetTriviaGameLobbyQuery` | Separar lectura de escritura | CQRS: query no modifica estado. |
| Observer / PubSub | SignalR Hub | Actualizar pantalla de espera en tiempo real | RF-13 exige tiempo real; SignalR es la tecnología aprobada (ADR-0005). |

No additional tactical pattern is introduced beyond the mandatory architectural patterns.
