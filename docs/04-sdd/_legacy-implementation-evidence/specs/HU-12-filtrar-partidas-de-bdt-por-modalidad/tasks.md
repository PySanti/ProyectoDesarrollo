# HU-12 — Tasks

## Task status key

- [ ] Pending
- [x] Done

## Backend dependency on HU-10

- [x] Reuse the BDT Game Service backend scaffold created for HU-10:
  - Domain project;
  - Application project;
  - Infrastructure project;
  - API project;
  - Unit, integration and contract test projects.
- [x] Reuse the `GET /api/bdt/games/published` endpoint instead of creating a second endpoint.
- [x] Reuse the published-games query/read repository and add modality filtering as an optional concern.
- [x] Keep HU-12 backend implementation inside BDT Game Service only.

## Domain

- [x] Ensure `PartidaBDT.Modalidad` is persisted and queryable.
- [x] Confirm `Modalidad` enum/value supports `Individual` and `Equipo` for BDT games.
- [x] Add parsing/validation support for external modality filter values without moving business rules to API controllers.
- [x] Ensure no domain mutation is introduced for filtering.

## Application

- [x] Extend/reuse `ListarPartidasBdtPublicadasQuery` with optional modality filter.
- [x] Add validation for modality values.
- [x] Return all published games when modality is omitted.
- [x] Return only matching published games when modality is provided.
- [x] Preserve the HU-10 invariant that only `Lobby` BDT games are returned, even when filtering by modality.
- [x] Map invalid modality values to an application validation result/exception for API `400` responses.
- [x] Ensure filtering remains a query-only operation with no persistence mutation and no event publication.

## Infrastructure

- [x] Reuse BDT published-games read model/repository from HU-10.
- [x] Apply optional modality filter in EF Core query.
- [x] Ensure omitted modality does not add a modality predicate.
- [x] Ensure `Individual` applies only `Modalidad == Individual`.
- [x] Ensure `Equipo` applies only `Modalidad == Equipo`.
- [x] Keep `Estado == Lobby` and modality filters composed in the same BDT read query.
- [x] Keep query scoped to BDT Game Service persistence.
- [x] Map persistence failures to consistent application/API handling.

## API

- [x] Support `modalidad` query parameter in `GET /api/bdt/games/published`.
- [x] Require authenticated participant access policy.
- [x] Map invalid modality to `400`.
- [x] Return `200` with filtered list payload.
- [x] Return the same response shape as HU-10 for all filter variants.
- [x] Ensure the endpoint does not create inscriptions, validate leadership or call Team Service.

## Contracts

- [x] Add HU-12 concrete filter behavior to `contracts/http/bdt-game-api.md`.
- [x] Document in `contracts/events/bdt-game-events.md` that no integration event is required for HU-12 closure.
- [x] Re-validate implemented payload/status against HU-12 contract definitions.

## Tests

- [x] Application/query handler tests for `Individual`, `Equipo` and omitted modality.
- [x] Validation test for invalid modality.
- [x] Application/query handler test proving modality filtering still excludes non-`Lobby` BDT games.
- [x] Infrastructure tests for EF Core optional modality filtering.
- [x] Infrastructure test proving omitted modality returns both `Individual` and `Equipo` published games.
- [x] Integration tests for `GET /api/bdt/games/published?modalidad=Individual|Equipo` and invalid modality `400`.
- [x] Integration test for omitted `modalidad` returning both modalities.
- [x] Contract tests for HU-12 endpoint query behavior and response shape.
- [x] Regression test proving filtering does not mutate BDT state or publish events.

## Frontend (React Native mobile)

- [x] Add filter controls in BDT published games panel.
- [x] Map `Todas` to omitted `modalidad` query parameter.
- [x] Map `Individual` to `modalidad=Individual`.
- [x] Map `Equipo` to `modalidad=Equipo`.
- [x] Render filtered empty state.
- [x] Render request error state.
- [x] Add mobile tests for filter selection and API parameter mapping.

## Acceptance and traceability

- [x] Update `docs/04-sdd/specs/HU-12-filtrar-partidas-de-bdt-por-modalidad/acceptance.md` with executed evidence after implementation.
- [x] Update HU-12 row in `docs/04-sdd/traceability-matrix.md` for SDD/contract readiness.
- [x] Align HU-12 status in `docs/04-sdd/SPECS-LIST.md` after implementation/testing.
- [x] Update HU-12 row in `docs/04-sdd/traceability-matrix.md` after implementation/testing.

## Hardening 10/10

- [x] Restore a reproducible backend test environment with the SDK required by `global.json` (`8.0.407`) or update the project SDK decision through an explicit repo-wide decision before verification.
- [x] Re-run and record fresh HU-12 backend evidence in `acceptance.md`:
  - `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.UnitTests/Umbral.BdtGameService.UnitTests.csproj --filter "FullyQualifiedName~ListarPartidasBdtPublicadas"`;
  - `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --filter "FullyQualifiedName~Hu10Hu12EndpointsIntegrationTests|FullyQualifiedName~PartidaBdtReadRepositoryTests"`;
  - `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.ContractTests/Umbral.BdtGameService.ContractTests.csproj --filter "FullyQualifiedName~Hu10Hu12ContractTests"`.
- [x] Add or record PostgreSQL/Npgsql runtime evidence for `GET /api/bdt/games/published?modalidad=Individual|Equipo`, proving modality filtering is executed against PostgreSQL and still excludes non-`Lobby` BDT games.
- [x] Restore mobile dependencies from `mobile/` and re-run HU-12 mobile evidence:
  - `npm run typecheck`;
  - `node --test tests/bdtPublishedGamesFlow.test.js tests/BdtPublishedGamesScreenController.test.js`.
- [x] Confirm invalid `modalidad` values return the documented `400` response and that changing filters does not create inscriptions, mutate BDT state or call Team Service.
- [x] Update `acceptance.md`, `docs/04-sdd/SPECS-LIST.md` and `docs/04-sdd/traceability-matrix.md` to mark HU-12 as `10/10` only after the fresh backend, mobile and PostgreSQL evidence is recorded.
