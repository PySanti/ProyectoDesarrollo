# HU-10 — Tasks

## Task status key

- [ ] Pending
- [x] Done

## Backend scaffold (BDT Game Service)

- [x] Create backend source structure for BDT Game Service if it does not exist:
  - `services/bdt-game-service/src/Umbral.BdtGameService.Domain`;
  - `services/bdt-game-service/src/Umbral.BdtGameService.Application`;
  - `services/bdt-game-service/src/Umbral.BdtGameService.Infrastructure`;
  - `services/bdt-game-service/src/Umbral.BdtGameService.Api`.
- [x] Create backend test structure for BDT Game Service if it does not exist:
  - `services/bdt-game-service/tests/Umbral.BdtGameService.UnitTests`;
  - `services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests`;
  - `services/bdt-game-service/tests/Umbral.BdtGameService.ContractTests`.
- [x] Wire project references so API depends on Application/Infrastructure, Application depends on Domain, and Infrastructure depends on Application/Domain.
- [x] Register BDT Game Service dependency injection for Application and Infrastructure.
- [x] Configure minimal API bootstrapping, authentication/authorization placeholders and health/runtime-safe startup without introducing non-approved services.

## Domain

- [x] Add `PartidaBDT` aggregate/entity with fields required by the published-games read use case:
  - `PartidaId`;
  - `Nombre`;
  - `Modalidad`;
  - `EstadoPartida`;
  - `AreaBusqueda`;
  - collection/count of `EtapaBDT`.
- [x] Add `EtapaBDT` entity or minimal owned child model needed to calculate `cantidadEtapas`.
- [x] Add `Modalidad` enum with `Individual` and `Equipo`.
- [x] Add `EstadoPartida` enum with at least `Lobby`, `Iniciada`, `Cancelada`, `Terminada`.
- [x] Add `AreaBusqueda` value object or validated primitive wrapper for textual search area.
- [x] Confirm `PartidaBDT` exposes read data needed for published listing:
  - `PartidaId`;
  - `Nombre`;
  - `Modalidad`;
  - `EstadoPartida`;
  - `AreaBusqueda`;
  - stages count.
- [x] Ensure no domain mutation is introduced for HU-10.

## Application

- [x] Create `ListarPartidasBdtPublicadasQuery` with optional `Modalidad` parameter.
- [x] Create response/read model for published BDT list items.
- [x] Add application port/read repository interface for published BDT listing.
- [x] Create query handler that returns only BDT games in `Lobby` state.
- [x] Return empty list when no published games exist.
- [x] Validate optional modality parameter when present.
- [x] Map invalid modality values to an application validation result/exception that the API can return as `400`.
- [x] Ensure the query handler performs no state mutation and publishes no domain/integration events.

## Infrastructure

- [x] Add `BdtDbContext` with PostgreSQL/EF Core configuration for `PartidaBDT` and `EtapaBDT`.
- [x] Add EF Core entity configuration for enum persistence and required fields.
- [x] Add BDT repository/read model method to list published games.
- [x] Implement EF Core/PostgreSQL query filtered by `Estado == Lobby`.
- [x] Project query results to the application read model without loading unrelated BDT gameplay data.
- [x] Keep query inside BDT Game Service database only.
- [x] Map persistence failures to consistent application/API handling.
- [x] Add local development database configuration consistent with the existing microservice conventions.

## API

- [x] Add `GET /api/bdt/games/published` endpoint in BDT Game Service API.
- [x] Require authenticated participant access policy.
- [x] Parse optional `modalidad` query parameter and pass it to the application query.
- [x] Map errors to HTTP statuses (`400`, `401`, `403`, `500`).
- [x] Return `200` with list payload.
- [x] Ensure response payload matches `contracts/http/bdt-game-api.md` exactly.
- [x] Ensure the endpoint does not create inscriptions, modify BDT state or call Team Service.

## Contracts

- [x] Add HU-10 endpoint section to `contracts/http/bdt-game-api.md`.
- [x] Document in `contracts/events/bdt-game-events.md` that no integration event is required for HU-10 closure.
- [x] Re-validate implemented payload/status against HU-10 contract definitions.

## Tests

- [x] Domain unit tests for BDT published-listing supporting fields and valid enum values.
- [x] Application/query handler tests for published listing.
- [x] Application/query handler test proving only `Lobby` games are returned.
- [x] Application/query handler test proving an empty list is returned when no published games exist.
- [x] Application/query handler test proving the query does not publish events or mutate state.
- [x] Infrastructure tests for EF Core projection and `Estado == Lobby` filtering.
- [x] Integration tests for `GET /api/bdt/games/published` (`200`, empty list, `401`, `403`).
- [x] Contract tests for HU-10 endpoint response shape and status codes.
- [x] Regression test proving no Team Service or non-BDT database access is required for listing.

## Frontend (React Native mobile)

- [x] Add BDT published games API client method.
- [x] Add BDT published games list screen or screen state.
- [x] Render loading state.
- [x] Render list state.
- [x] Render empty state.
- [x] Render error state.
- [x] Add mobile tests for happy path, empty state and request error.

## Acceptance and traceability

- [x] Update `docs/04-sdd/specs/HU-10-ver-partidas-de-bdt-publicadas/acceptance.md` with executed evidence after implementation.
- [x] Update HU-10 row in `docs/04-sdd/traceability-matrix.md` for SDD/contract readiness.
- [x] Align HU-10 status in `docs/04-sdd/SPECS-LIST.md` after implementation/testing.
- [x] Update HU-10 row in `docs/04-sdd/traceability-matrix.md` after implementation/testing.

## Hardening 10/10

- [x] Restore a reproducible backend test environment with the SDK required by `global.json` (`8.0.407`) or update the project SDK decision through an explicit repo-wide decision before verification.
- [x] Re-run and record fresh HU-10 backend evidence in `acceptance.md`:
  - `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.UnitTests/Umbral.BdtGameService.UnitTests.csproj`;
  - `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --filter "FullyQualifiedName~Hu10Hu12EndpointsIntegrationTests|FullyQualifiedName~PartidaBdtReadRepositoryTests"`;
  - `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.ContractTests/Umbral.BdtGameService.ContractTests.csproj --filter "FullyQualifiedName~Hu10Hu12ContractTests"`.
- [x] Add or record PostgreSQL/Npgsql runtime evidence for `GET /api/bdt/games/published`, using `ConnectionStrings:BdtDatabase`, proving the endpoint returns only `Lobby` BDT games from PostgreSQL and not from EF Core InMemory.
- [x] Restore mobile dependencies from `mobile/` and re-run HU-10 mobile evidence:
  - `npm run typecheck`;
  - `node --test tests/bdtPublishedGamesFlow.test.js tests/BdtPublishedGamesScreenController.test.js`.
- [x] Confirm the published-games query remains read-only by recording no inscription creation, no BDT state mutation and no Team Service/database access in the fresh HU-10 test evidence.
- [x] Update `acceptance.md`, `docs/04-sdd/SPECS-LIST.md` and `docs/04-sdd/traceability-matrix.md` to mark HU-10 as `10/10` only after the fresh backend, mobile and PostgreSQL evidence is recorded.
