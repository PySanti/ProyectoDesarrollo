# HU-23 — Acceptance

## Current status

Backend implemented and documented. React web operator UI integrated in current pass.

## Acceptance Checklist

| Criterio | Verificado | Evidencia |
| --- | --- | --- |
| Operator query returns unique team IDs | Backend verificado | `GetTriviaGameTeamsQueryHandlerTests.Handle_PartidaConEquipos_RetornaEquiposUnicos` |
| Query returns empty list when no teams are registered | Backend verificado | `GetTriviaGameTeamsQueryHandlerTests.Handle_PartidaSinEquipos_RetornaListaVacia` |
| Missing game returns not found | Backend verificado | `GetTriviaGameTeamsQueryHandlerTests.Handle_PartidaNoExiste_ThrowsPartidaTriviaNotFoundException` |
| Endpoint documented | Verificado | `contracts/http/trivia-game-api.md` section `GET /api/trivia-games/{id}/teams` |
| React web UI navigable | Verificado | `TriviaOperationsPage` calls `GET /api/trivia-games/{id}/teams` |

## Automated Evidence

| Suite | Status | Evidence |
| --- | --- | --- |
| Application tests | Passed in Trivia backend suite | `GetTriviaGameTeamsQueryHandlerTests` |
| API tests | Passed in Trivia backend suite | `TriviaGamesControllerTests` team endpoint coverage |
| Frontend tests | Passed | Covered by `TriviaOperationsPage` render and frontend suite |

## Integration pass evidence

- Validation run: `npm test --prefix frontend` → 43 passed.
- Validation run: `npm run build --prefix frontend` → passed.

## Traceability

- SPECS-LIST: aligned as backend done / web operator UI integrated.
- Traceability matrix: aligned as backend done / web operator UI integrated.
- Contracts: `contracts/http/trivia-game-api.md`.
