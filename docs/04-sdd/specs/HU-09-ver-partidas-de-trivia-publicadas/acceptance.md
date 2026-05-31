# HU-09 — Acceptance: Ver partidas de Trivia publicadas

## Acceptance criteria checklist

| ID | Criterio | Estado | Evidencia |
| --- | --- | --- | --- |
| CA-01 | Participante autenticado recibe lista de partidas en estado Lobby | ✅ | `TriviaGamesPublicControllerTests.GetAll_AsParticipante_Returns200WithList`, `TriviaGamesPublicControllerTests.GetAll_AsOperador_Returns200WithList` |
| CA-02 | Lista vacía si no hay partidas publicadas | ✅ | `TriviaGamesPublicControllerTests.GetAll_NoGames_ReturnsEmptyList`, `GetPublishedTriviaGamesQueryHandlerTests.Handle_NoPublishedGames_ReturnsEmptyList` |
| CA-03 | Solo retorna partidas en estado Lobby | ✅ | `GetPublishedTriviaGamesQueryHandlerTests.Handle_ReturnsOnlyLobbyGames` |

## Backend items checklist

| Item | Estado |
| --- | --- |
| DTO `TriviaGameListItemDto` creado | ✅ |
| Método `GetPublishedAsync` en `IPartidaTriviaRepository` | ✅ |
| Query `GetPublishedTriviaGamesQuery` creado | ✅ |
| Handler `GetPublishedTriviaGamesQueryHandler` implementado | ✅ |
| Mapper `ToListItemDto` en `TriviaGameMapper` | ✅ |
| Repositorio `PartidaTriviaRepository.GetPublishedAsync` implementado | ✅ |
| Controller `TriviaGamesPublicController` con `GET /api/trivia-games` | ✅ |
| Contrato HTTP actualizado | ✅ |
| Tests unitarios Application (2) | ✅ |
| Tests integración API (3) | ✅ |
| Compilación y tests correctos | ✅ |

## Reglas de negocio verificadas

| Regla | Verificación |
| --- | --- |
| RF-05: mostrar partidas publicadas a participantes | El endpoint GET devuelve lista de partidas en Lobby para cualquier autenticado |
| RB-G02: estados válidos | Solo se listan partidas con Estado == Lobby |

## Total de pruebas

| Proyecto | Tests pasados | Tests agregados por HU-09 |
| --- | --- | --- |
| Domain.Tests | 130 | 0 |
| Application.Tests | 67 | 2 |
| Api.Tests | 19 | 3 |
| **Total** | **216** | **5** |
