# HU-22 — Acceptance

## Current status

Backend implemented and documented. React web operator UI integrated in current pass.

## Acceptance Checklist

| Criterio | Verificado | Evidencia |
| --- | --- | --- |
| Operator query returns lobby participant list | Backend verificado | `GetTriviaGameParticipantsQueryHandlerTests.Handle_PartidaExiste_RetornaDtoConParticipantes` |
| Missing game returns not found | Backend verificado | `GetTriviaGameParticipantsQueryHandlerTests.Handle_PartidaNoExiste_ThrowsPartidaTriviaNotFoundException` |
| Endpoint documented | Verificado | `contracts/http/trivia-game-api.md` section `GET /api/trivia-games/{id}/participants` |
| Query does not mutate state | Verificado por diseño | CQRS query and repository read path only |
| React web UI navigable | Verificado | `TriviaOperationsPage` calls `GET /api/trivia-games/{id}/participants` |

## Automated Evidence

| Suite | Status | Evidence |
| --- | --- | --- |
| Application tests | Passed in Trivia backend suite | `GetTriviaGameParticipantsQueryHandlerTests` |
| API tests | Passed in Trivia backend suite | `TriviaGamesControllerTests` participant endpoint coverage |
| Frontend tests | Passed | `TriviaOperationsPage.test.tsx` participant load case |

## Integration pass evidence

- Validation run: `npm test --prefix frontend` → 43 passed.
- Validation run: `npm run build --prefix frontend` → passed.

## Traceability

- SPECS-LIST: aligned as backend done / web operator UI integrated.
- Traceability matrix: aligned as backend done / web operator UI integrated.
- Contracts: `contracts/http/trivia-game-api.md`.
