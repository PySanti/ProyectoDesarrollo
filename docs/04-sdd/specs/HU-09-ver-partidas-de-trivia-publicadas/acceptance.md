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

## Mobile frontend items checklist

| Item | Estado |
| --- | --- |
| Proyecto React Native (Expo + TypeScript) inicializado | ✅ |
| API client con Axios e interceptor JWT | ✅ |
| DTO `TriviaGameListItem` en mobile | ✅ |
| API service `getPublishedTriviaGames()` | ✅ |
| Pantalla `TriviaGamesListScreen` con loading/empty/error | ✅ |
| Componente `ScreenWrapper` compartido | ✅ |
| Navegador `AppNavigator` con stack principal | ✅ |
| Compilación TypeScript sin errores | ✅ |
| Pruebas mobile para URL/listado y etiquetas de capacidad | ✅ |

## Total de pruebas

| Proyecto | Tests pasados | Tests agregados por HU-09 |
| --- | --- | --- |
| Domain.Tests | 130 | 0 |
| Application.Tests | 67 | 2 |
| Api.Tests | 19 | 3 |
| Mobile tests | 81 | 4 compartidos HU-09/HU-11 |
| **Total** | **216 backend + 81 mobile** | **5 backend + 4 mobile compartidos** |

## Integration pass evidence

- `mobile/src/features/trivia/triviaPublishedGamesModel.js` centralizes published Trivia list URL/filter presentation helpers.
- `mobile/tests/triviaPublishedGamesFlow.test.js` verifies HU-09 no-filter URL and individual/team capacity labels.
- `mobile/src/api/triviaApi.ts` uses the shared URL helper for `GET /api/trivia-games`.
- Validation run: `npm test --prefix mobile` → 81 passed.
- Validation run: `npm run typecheck --prefix mobile` → passed.
