# HU-43 - Tasks

## Planning Decisions for 10/10

- [x] Keep HU-43 owned by BDT Game Service.
- [x] Keep Team Service out of HU-43; BDT registrations are evaluated from BDT Game Service state.
- [x] Resolve SignalR/WebSocket post-commit behavior: dispatch failure is logged, does not roll back persisted state and does not convert a successful start into HTTP `500`.
- [x] Use `PartidaBDTIniciada` as the HU-43 SignalR/WebSocket message name.
- [x] Require backend/server timestamps for active-stage timer calculation.
- [x] Keep RabbitMQ out of HU-43 closure because there is no approved cross-service consumer.

## Domain

- [x] Add or verify `PartidaBDT.IniciarManualmente(...)` for `Lobby` to `Iniciada` transition.
- [x] Add or verify `EtapaBDT.Activar(...)` with server timestamps.
- [x] Validate configured minimum participation before start.
- [x] Validate manual start against `ModoInicioPartida`.
- [x] Ensure exactly one active stage after successful start.
- [x] Ensure BDT start does not create numeric BDT score/ranking state.

## Application

- [x] Add `IniciarPartidaBdtCommand`.
- [x] Add command validator for empty `PartidaId` and `OperadorUserId`.
- [x] Add command handler that loads aggregate with stages and registrations.
- [x] Persist game state and active-stage state atomically.
- [x] Return started game and active-stage response DTO.
- [x] Add application port for BDT real-time state/stage update.
- [x] Catch and log post-commit real-time dispatch failures without failing the already-persisted command.

## Infrastructure

- [x] Add EF Core mapping for active-stage state and timer timestamps if missing.
- [x] Add repository load method including stages and registrations.
- [x] Add repository update method for BDT start transition.
- [x] Implement SignalR/WebSocket adapter for game-started update after persistence.
- [x] Add PostgreSQL persistence coverage using isolated schema.

## API

- [x] Add `POST /api/bdt/games/{partidaId}/start` endpoint.
- [x] Enforce authenticated `Operador` authorization.
- [x] Derive `OperadorUserId` from authenticated token claims only.
- [x] Return documented `400`, `401`, `403`, `404`, `409` and pre-commit `500` cases.
- [x] Keep endpoint free of business rules.

## Contracts

- [x] Update `contracts/http/bdt-game-api.md` with HU-43 start endpoint.
- [x] Update `contracts/events/bdt-game-events.md` with HU-43 no-RabbitMQ decision and SignalR payload decision.
- [x] Document real-time update name, payload and post-commit dispatch failure behavior before implementation.
- [x] Keep contracts synchronized if implementation discovers a necessary shape change.

## Tests

- [x] Add domain unit tests for valid start and all start rejection rules.
- [x] Add application handler tests for persistence and real-time port invocation.
- [x] Add application handler test for post-commit real-time failure returning success with persisted state.
- [x] Add API integration tests for success and documented errors.
- [x] Add HTTP contract tests.
- [x] Add real-time adapter tests or integration tests.
- [x] Add PostgreSQL/Npgsql persistence tests.

## 10/10 Hardening Plan

- [x] Require authentication/authorization on `/hubs/bdt` so only authenticated UMBRAL users can connect.
- [x] Replace broadcast-to-all behavior with partida-scoped SignalR groups or equivalent authorized targeting.
- [x] Ensure participants/operators can only receive `PartidaBDTIniciada` for BDT games they are allowed to observe.
- [x] Add a BDT-owned read/application authorization check for SignalR group subscription, using `PartidaId`, authenticated `UserId` and role claims.
- [x] Update `BdtPartidaHub.SubscribeToPartida` to call the authorization check before `Groups.AddToGroupAsync` and reject unauthorized subscriptions with `HubException` or equivalent SignalR error.
- [x] Add SignalR integration test proving an authenticated participant not registered in the requested BDT game cannot subscribe to that partida group and cannot receive `PartidaBDTIniciada`.
- [x] Add SignalR integration test proving a registered/active participant can subscribe and receive `PartidaBDTIniciada` for that partida.
- [x] Add SignalR integration test proving an authenticated operator can subscribe for supervision.
- [x] Serialize concurrent start attempts for the same `PartidaBDT` using a PostgreSQL transaction/advisory lock or an EF Core concurrency token.
- [x] Add a concurrent start PostgreSQL/Npgsql test proving exactly one request starts the game and the competing request returns `409` without duplicate stage activation.
- [x] Add a real SignalR adapter/payload test that verifies the documented `PartidaBDTIniciada` shape, not only a fake notifier invocation.
- [x] Align `design.md`, `contracts/events/bdt-game-events.md` and implementation on the final `PartidaBDTIniciada` payload fields.
- [x] Re-run backend unit, integration, contract, PostgreSQL concurrency, frontend tests and frontend build after subscription-authorization hardening.
- [x] Update `acceptance.md`, `SPECS-LIST.md` and `traceability-matrix.md` only after the subscription-authorization hardening evidence passes.

## 10/10 Verification Target

- [x] Domain unit tests cover all start invariants and BDT ranking non-creation.
- [x] Application tests cover success, not found, conflicts, persistence, real-time success and real-time failure.
- [x] API integration tests cover documented `200`, `400`, `401`, `403`, `404`, `409`, persistence behavior and concurrent-start conflict behavior.
- [x] Contract tests verify the HU-43 HTTP response and error contract.
- [x] PostgreSQL tests verify state, stage timestamp persistence and concurrent-start safety with isolated schema.
- [x] React web tests verify operator start action, loading state, duplicate-click prevention, success and business errors.
- [x] Real-time tests verify documented `PartidaBDTIniciada` payload, authorized/scoped delivery, rejection of unauthorized participant subscription and no notification on failed starts.
- [x] Acceptance evidence records exact commands and passing counts before the HU is marked 10/10 again.

## Frontend Web

- [x] Add operator start action in the BDT operator flow.
- [x] Add API client method for `POST /api/bdt/games/{partidaId}/start`.
- [x] Render loading, success and error states.
- [x] Prevent duplicate start taps while request is in flight.

## Acceptance and Traceability

- [x] Update `acceptance.md` with executed evidence after subscription-authorization hardening.
- [x] Update `docs/04-sdd/traceability-matrix.md` after subscription-authorization status changes.
- [x] Update `docs/04-sdd/SPECS-LIST.md` after subscription-authorization status changes.
