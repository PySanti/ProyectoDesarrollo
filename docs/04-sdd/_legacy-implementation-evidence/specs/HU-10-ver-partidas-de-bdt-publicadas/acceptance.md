# HU-10 — Acceptance

## Acceptance checklist

- [x] HU-10 is assigned to `BDT Game Service`.
- [x] HU-10 is implemented for `React Native mobile` participant flow.
- [x] Participant can view published BDT games in the mobile BDT panel.
- [x] The query returns only BDT games in `Lobby` state.
- [x] Individual and team BDT games are both visible without filtering.
- [x] Empty state is shown when no BDT games are published.
- [x] Request error state is shown without inventing backend rules.
- [x] The query does not create inscriptions or modify game state.
- [x] HU-10 contract section is documented in `contracts/http/bdt-game-api.md`.
- [x] Traceability row for HU-10 is updated.

## Manual verification steps

1. Authenticate as participant in the mobile app.
2. Open the Busqueda del Tesoro panel.
3. Verify published BDT games are listed.
4. Verify both `Individual` and `Equipo` modalities are visible when available.
5. Verify each card shows name, modality, state, search area and stage count.
6. Verify empty state when no published BDT exists.
7. Verify a backend/API error shows a clear error state.
8. Verify no inscription is created by opening the list.

## Automated test evidence

Commands to execute after implementation:

- `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.UnitTests/Umbral.BdtGameService.UnitTests.csproj`
- `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --filter Hu10`
- `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.ContractTests/Umbral.BdtGameService.ContractTests.csproj --filter Hu10`
- `npm run typecheck` from `mobile/`
- `npm test` from `mobile/`

Latest execution summary:

- Mobile implementation executed for the participant BDT panel using the documented `GET /api/bdt/games/published` contract.
- `node --test tests/bdtPublishedGamesFlow.test.js tests/BdtPublishedGamesScreenController.test.js` from `mobile/` — Passed: 9/9.
- `npm run typecheck` from `mobile/` — Passed.
- Backend BDT Game Service code and focused tests exist in this workspace. The HU-10 backend task checklist is closed and fresh PostgreSQL/Npgsql runtime evidence is recorded.
- `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.ContractTests/Umbral.BdtGameService.ContractTests.csproj --filter "FullyQualifiedName~Hu10Hu12ContractTests"` — Passed: 4/4; HU-10 payload/status revalidated against `contracts/http/bdt-game-api.md`.
- `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.UnitTests/Umbral.BdtGameService.UnitTests.csproj` — Passed: 16/16; HU-10 domain read fields, enums, stages and `AreaBusqueda` verified.
- `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.UnitTests/Umbral.BdtGameService.UnitTests.csproj --filter "FullyQualifiedName~ListarPartidasBdtPublicadas"` — Passed: 12/12; HU-10 application query, read model, repository port usage, optional modality validation and invalid-modality mapping verified.
- `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --filter "FullyQualifiedName~Hu10Hu12EndpointsIntegrationTests|FullyQualifiedName~PartidaBdtReadRepositoryTests"` — Passed: 12/12; published-listing endpoint and repository filtering verified with EF Core InMemory.
- `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --filter "FullyQualifiedName~Hu10Hu12EndpointsIntegrationTests"` — Passed: 7/7; empty-list and `Lobby`-only endpoint behavior verified.
- `BDT_POSTGRES_TEST_CONNECTION='Host=localhost;Port=5432;Database=umbral_bdt_game;Username=postgres;Password=postgres;' dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --filter "FullyQualifiedName~Hu10PostgresPublishedGamesTests" --no-restore` — Passed: 1/1; verified `GET /api/bdt/games/published` returns only `Lobby` BDT games from PostgreSQL/Npgsql and does not mutate persisted BDT state.
- `global.json` uses SDK `8.0.407` with `rollForward: latestMajor`, allowing reproducible verification on the installed SDK while preserving the project target framework.

## Readiness evidence

- SDD files exist: `spec.md`, `design.md`, `tasks.md`, `acceptance.md`.
- HTTP contract documented: `GET /api/bdt/games/published` in `contracts/http/bdt-game-api.md`.
- Event-contract note documented: `contracts/events/bdt-game-events.md` states no integration event is required for HU-10 closure unless implementation introduces one.
- Traceability row documents SDD and contract readiness.
- Mobile API client and screen state are implemented against the documented contract.
- BDT domain scaffold exists for HU-10 listing: `PartidaBDT`, `EtapaBDT`, `Modalidad`, `EstadoPartida` and `AreaBusqueda`.
- BDT application layer exists for HU-10 listing: `ListarPartidasBdtPublicadasQuery`, `PartidaBdtPublicadaItem`, `IPartidaBdtReadRepository`, validator and handler.
- BDT API layer exposes `GET /api/bdt/games/published` with participant authorization, optional `modalidad` parsing, documented status mapping and contract-aligned response shape.
- BDT development configuration includes a local PostgreSQL connection string under `ConnectionStrings:BdtDatabase`.
- PostgreSQL/Npgsql runtime evidence exists for the published BDT games endpoint and verifies read-only behavior.

## Traceability status

- HU-10 currently appears in `docs/04-sdd/SPECS-LIST.md` as `10/10` with backend, mobile, contract and PostgreSQL verification.
- HU-10 appears in `docs/04-sdd/traceability-matrix.md` with mobile, backend, integration, contract and PostgreSQL/Npgsql evidence complete.

## Assumptions

- Published BDT means BDT game in `Lobby` state.
- Real-time publication updates are not required to close HU-10.
- HU-12 reuses the same endpoint with an optional modality query parameter.
