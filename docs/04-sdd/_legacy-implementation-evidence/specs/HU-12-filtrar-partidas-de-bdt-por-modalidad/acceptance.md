# HU-12 — Acceptance

## Acceptance checklist

- [x] HU-12 is assigned to `BDT Game Service`.
- [x] HU-12 is implemented for `React Native mobile` participant flow.
- [x] Participant can filter BDT published games by `Todas`, `Individual` and `Equipo`.
- [x] `Todas` maps to the published BDT query without `modalidad`.
- [x] `Individual` shows only individual published BDT games.
- [x] `Equipo` shows only team published BDT games.
- [x] Invalid modality query is rejected with `400`.
- [x] Empty state is shown for a filter with no results.
- [x] Filtering does not create inscriptions or modify BDT games.
- [x] HU-12 contract behavior is documented in `contracts/http/bdt-game-api.md`.
- [x] Traceability row for HU-12 is updated.

## Manual verification steps

1. Authenticate as participant in the mobile app.
2. Open the Busqueda del Tesoro panel.
3. Select `Todas` and verify individual and team games can appear.
4. Select `Individual` and verify only individual BDT games appear.
5. Select `Equipo` and verify only team BDT games appear.
6. Verify empty state for a modality with no results.
7. Verify changing filters does not create an inscription.
8. Verify invalid modality through API test setup returns `400`.

## Automated test evidence

Commands to execute after implementation:

- `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.UnitTests/Umbral.BdtGameService.UnitTests.csproj`
- `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --filter Hu12`
- `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.ContractTests/Umbral.BdtGameService.ContractTests.csproj --filter Hu12`
- `npm run typecheck` from `mobile/`
- `npm test` from `mobile/`

Latest execution summary:

- Mobile implementation executed for BDT modality filters using the documented `modalidad` query contract.
- `node --test tests/bdtPublishedGamesFlow.test.js tests/BdtPublishedGamesScreenController.test.js` from `mobile/` — Passed: 9/9.
- `npm run typecheck` from `mobile/` — Passed.
- Backend BDT Game Service code and focused tests exist in this workspace. The HU-12 backend task checklist is closed and fresh PostgreSQL/Npgsql runtime evidence is recorded.
- `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.ContractTests/Umbral.BdtGameService.ContractTests.csproj --filter "FullyQualifiedName~Hu10Hu12ContractTests"` — Passed: 4/4; HU-12 query payload/status revalidated against `contracts/http/bdt-game-api.md`.
- `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.UnitTests/Umbral.BdtGameService.UnitTests.csproj` — Passed: 16/16; modality enum and modality parser/validator verified.
- `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.UnitTests/Umbral.BdtGameService.UnitTests.csproj --filter "FullyQualifiedName~ListarPartidasBdtPublicadas"` — Passed: 12/12; HU-12 optional modality query reuse, validation and omitted-modality application behavior verified.
- `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --filter "FullyQualifiedName~Hu10Hu12EndpointsIntegrationTests|FullyQualifiedName~PartidaBdtReadRepositoryTests"` — Passed: 12/12; optional modality filtering verified with EF Core InMemory.
- `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --filter "FullyQualifiedName~Hu10Hu12EndpointsIntegrationTests"` — Passed: 7/7; omitted-modality endpoint behavior and modality filter endpoint behavior verified.
- `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --filter "FullyQualifiedName~Hu10Hu12EndpointsIntegrationTests|FullyQualifiedName~PartidaBdtReadRepositoryTests"` — Passed: 12/12; HU-12 repository, endpoint filtering, invalid modality and no-mutation regression verified.
- `BDT_POSTGRES_TEST_CONNECTION='Host=localhost;Port=5432;Database=umbral_bdt_game;Username=postgres;Password=postgres;' dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --filter "FullyQualifiedName~Hu12PostgresModalityFilterTests" --no-restore` — Passed: 3/3; verified `modalidad=Individual|Equipo` filtering against PostgreSQL/Npgsql, exclusion of non-`Lobby` BDT games, invalid modality `400`, and read-only behavior.
- `node --test tests/bdtPublishedGamesFlow.test.js tests/BdtPublishedGamesScreenController.test.js` from `mobile/` — Passed: 9/9; mobile filter mapping and filtered UI state verified.
- `global.json` uses SDK `8.0.407` with `rollForward: latestMajor`, allowing reproducible verification on the installed SDK while preserving the project target framework.

## Readiness evidence

- SDD files exist: `spec.md`, `design.md`, `tasks.md`, `acceptance.md`.
- HTTP contract behavior documented: `GET /api/bdt/games/published?modalidad=Individual|Equipo` in `contracts/http/bdt-game-api.md`.
- Event-contract note documented: `contracts/events/bdt-game-events.md` states no integration event is required for HU-12 closure unless implementation introduces one.
- Traceability row documents SDD and contract readiness.
- Mobile filter controls and API parameter mapping are implemented against the documented contract.
- BDT modality filter support exists in application parser/validator and the persisted/queryable `PartidaBDT.Modalidad` field.
- HU-12 reuses `ListarPartidasBdtPublicadasQuery` with optional `Modalidad`, preserving the same HU-10 response shape.
- PostgreSQL/Npgsql runtime evidence exists for modality filtering and verifies read-only behavior.

## Traceability status

- HU-12 currently appears in `docs/04-sdd/SPECS-LIST.md` as `10/10` with backend, mobile, contract and PostgreSQL verification.
- HU-12 appears in `docs/04-sdd/traceability-matrix.md` with mobile, backend, integration, contract and PostgreSQL/Npgsql evidence complete.

## Assumptions

- HU-12 reuses the HU-10 published-games endpoint.
- The filter `Todas` omits the `modalidad` query parameter.
- Backend filtering remains authoritative; mobile only maps UI filter state to documented query parameters.
