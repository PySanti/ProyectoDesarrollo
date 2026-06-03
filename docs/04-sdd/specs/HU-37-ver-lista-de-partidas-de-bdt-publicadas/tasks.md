# HU-37 - Tasks

## Domain

- [x] Verify published BDT state is represented consistently as `Lobby` for first delivery.
- [x] Ensure no domain method is required because this HU is read-only.

## Application

- [x] Add `ListarPartidasBdtPublicadasOperadorQuery`.
- [x] Add query handler returning operator list read model.
- [x] Ensure query filters only published BDT games.

## Infrastructure

- [x] Add repository/query implementation for operator published BDT list.
- [x] Ensure EF Core query includes stage count efficiently.
- [x] Add PostgreSQL/Npgsql test coverage using isolated test database/schema.

## API

- [x] Add `GET /api/bdt/operator/games/published` endpoint.
- [x] Enforce authenticated `Operador` authorization.
- [x] Keep endpoint read-only and free of business rules.

## Contracts

- [x] Update `contracts/http/bdt-game-api.md` with operator list endpoint after SDD review.
- [x] Update `contracts/events/bdt-game-events.md` to state no integration event is required for HU-37 closure.
- [x] Document that no SignalR behavior is required for HU-37 closure.

## Tests

- [x] Add application query tests.
- [x] Add API authorization tests.
- [x] Add API response mapping tests.
- [x] Add HTTP contract tests.
- [x] Add React web list tests.

## Frontend Web

- [x] Add operator BDT published games list screen or route.
- [x] Add API client method for operator published BDT list.
- [x] Render name and state for each game.
- [x] Render loading, empty and error states.

## Acceptance and Traceability

- [x] Update `acceptance.md` with evidence after implementation.
- [x] Update `docs/04-sdd/traceability-matrix.md` after implementation status changes.
- [x] Update `docs/04-sdd/SPECS-LIST.md` after implementation status changes.

## Hardening to Reach Strict 10/10

- [x] Add HU-37 API/integration coverage for authenticated `Operador` requests with missing or malformed `sub` claim, verifying the endpoint rejects the request without returning data.
- [x] Add HU-37-specific test coverage for deterministic ordering of the operator list (`Nombre` then `PartidaId`) or document a different ordering decision in `design.md` and the HTTP contract.
- [x] Add frontend API client tests for `getOperatorPublishedBdtGames`, verifying it calls `GET /api/bdt/operator/games/published`, sends the bearer token and maps non-OK responses to `BdtApiError`.
- [x] Add React web handling and tests for `401` from HU-37 with a clear unauthenticated/session-expired message, separate from the existing `403` operator-role message.
- [x] Add an accessible table caption or equivalent `aria-label` to the HU-37 published BDT games table and cover it in the React test.
- [x] Add a manual/runtime smoke evidence entry for React web HU-37 with real Keycloak `Operador` token against BDT Game Service, or explicitly document why it remains environment-dependent.
- [x] Re-run and record final evidence after hardening: HU-37 unit, HU-37 integration, HU-37 contract, HU-37 PostgreSQL, frontend tests and frontend build.
- [x] Update `acceptance.md`, `docs/04-sdd/traceability-matrix.md` and `docs/04-sdd/SPECS-LIST.md` after these hardening tasks are completed.
