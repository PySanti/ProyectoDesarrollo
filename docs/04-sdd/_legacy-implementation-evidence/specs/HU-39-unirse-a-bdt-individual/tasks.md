# HU-39 - Tasks

## Planning Gaps To Close For 10/10

- [x] Preserve BDT Game Service ownership: implement HU-39 only in BDT Game Service plus React Native mobile, with Identity/Keycloak token claims as support only.
- [x] Keep Team Service completely out of HU-39: no HTTP client, no leadership validation and no team data mutation.
- [x] Cover duplicate join with PostgreSQL-backed tests, not only in-memory/domain tests.
- [x] Cover full-capacity race conditions with a PostgreSQL-backed concurrent join test using different participant ids.
- [x] Prove API authorization edge cases: unauthenticated `401`, non-participant `403`, and missing/malformed participant `sub` claim rejected without mutation.
- [x] Prove mobile behavior at controller/flow level: join action, loading state, success waiting screen, duplicate/capacity/error messages and no team-mode join action through this endpoint.
- [x] Record final 10/10 hardening evidence in `acceptance.md`, `traceability-matrix.md` and `SPECS-LIST.md` before marking HU-39 as completed.

## Domain

- [x] Add `ExploradorBDT` domain entity, or verify an equivalent existing entity, with `ExploradorId`, `CompetidorId`, `TipoCompetidor.Usuario` and lobby registration data required by HU-39.
- [x] Add or verify `PartidaBDT.RegistrarParticipanteIndividual(Guid participanteUserId)` as the only domain entry point for individual BDT registration.
- [x] Validate `EstadoPartida.Lobby` before accepting an individual registration.
- [x] Validate `Modalidad.Individual`; reject team-modality games through the individual registration method.
- [x] Validate configured `MaximoParticipantes` and reject registrations when individual capacity is full.
- [x] Reject duplicate registration by the same participant in the same BDT game.
- [x] Compute deterministic `posicionEnLobby` from persisted individual registrations after successful registration.
- [x] Keep HU-39 free of BDT ranking/scoring fields; do not introduce numeric BDT score for joining.

## Application

- [x] Add `UnirseABdtIndividualCommand` with `PartidaId` and authenticated `ParticipanteUserId`.
- [x] Add `UnirseABdtIndividualCommandValidator` for empty `PartidaId` and empty `ParticipanteUserId`.
- [x] Add `UnirseABdtIndividualCommandHandler` that loads the aggregate, invokes domain registration, persists changes and returns waiting-screen data.
- [x] Add `UnirseABdtIndividualResponse` matching the HTTP contract: `partidaId`, `nombre`, `modalidad`, `estado`, `inscripcionId`, `participanteUserId`, `posicionEnLobby`, `mensaje`.
- [x] Map not-found game to application `404` result.
- [x] Map invalid state, invalid modality, duplicate participant and full capacity to business `409` results.
- [x] Keep handler orchestration-only; business rules must remain in domain methods or explicit domain services.
- [x] Do not publish RabbitMQ events or SignalR updates in HU-39; keep those deferred as documented.

## Infrastructure And Persistence

- [x] Add EF Core mapping for individual `ExploradorBDT` registrations associated to `PartidaBDT`.
- [x] Add a repository load method that includes current BDT registrations/explorers for the target `PartidaBDT`.
- [x] Add a repository save/update path for aggregate changes caused by individual registration.
- [x] Add database-level uniqueness protection for `(partida_id, competidor_id, tipo_competidor)` to reject duplicate individual registrations safely.
- [x] Map database unique-constraint conflicts for duplicate registration to a domain/application `409`, not `500`.
- [x] Add PostgreSQL-safe concurrency control so capacity checks execute against locked/current persisted registrations before commit.
- [x] Map the losing concurrent full-capacity join to the documented `409` business response, not `500`.
- [x] Verify capacity checks remain valid with PostgreSQL-backed concurrent execution by different participant ids.
- [x] Use isolated PostgreSQL/Npgsql schema strategy consistent with HU-34/HU-37 hardening, avoiding destructive shared schema assumptions.

## API

- [x] Add `POST /api/bdt/games/{partidaId}/individual-inscriptions` endpoint in BDT Game Service.
- [x] Enforce authenticated `Participante` authorization policy.
- [x] Accept no request body; derive `participanteUserId` only from authenticated token claims.
- [x] Return `400` for invalid `partidaId` path input.
- [x] Return `401` when unauthenticated.
- [x] Return `403` for authenticated non-participant users and for missing/malformed participant `sub` claim.
- [x] Return `404` for missing `PartidaBDT`.
- [x] Return `409` for non-lobby, team-modality, duplicate and capacity-full conflicts.
- [x] Return `200 OK` with waiting-screen DTO on success.
- [x] Keep the endpoint/controller free of business rules; it may only parse claims, send MediatR command and map results.

## Contracts

- [x] Update `contracts/http/bdt-game-api.md` with individual join endpoint after SDD review.
- [x] Update `contracts/events/bdt-game-events.md` to document that HU-39 publishes no integration event for closure.
- [x] Document that SignalR lobby updates are deferred to HU-42 or HU-55 for HU-39 readiness.
- [x] After implementation, verify the final response shape and error mapping still match `contracts/http/bdt-game-api.md`.
- [x] If implementation adds any field, status code or event not already documented, update the SDD and contracts before marking completion.

## Backend Tests

- [x] Add domain unit tests for successful individual registration in `Lobby` with capacity.
- [x] Add domain unit tests for non-lobby rejection.
- [x] Add domain unit tests for team-modality rejection.
- [x] Add domain unit tests for duplicate participant rejection.
- [x] Add domain unit tests for full-capacity rejection.
- [x] Add application handler tests for success persistence and waiting-screen DTO.
- [x] Add application handler tests for not-found and all business conflict mappings.
- [x] Add API integration tests for `200`, `400`, `401`, `403`, `404` and all `409` variants.
- [x] Add API integration test proving no request-body participant id can override the authenticated token participant id.
- [x] Add HTTP contract tests for the success response shape and documented errors.
- [x] Add PostgreSQL/Npgsql test proving registration is persisted with isolated schema.
- [x] Add PostgreSQL/Npgsql duplicate-registration test proving unique constraint/conflict mapping works.
- [x] Add PostgreSQL/Npgsql sequential full-capacity test proving capacity is rejected under database-backed execution.
- [x] Add PostgreSQL/Npgsql concurrent full-capacity test with `MaximoParticipantes = 1` and two different participant ids; assert exactly one success, one `409`, and one persisted `ExploradorBDT`.
- [x] Run full BDT unit, integration and contract suites after HU-39 concurrency hardening.

## Mobile

- [x] Add mobile API client method for `POST /api/bdt/games/{partidaId}/individual-inscriptions` using bearer token and no body.
- [x] Map mobile API responses: success, `401`, `403`, `404`, `409`, network failure and generic server error.
- [x] Add mobile flow/model function that joins only `Individual` BDT games and blocks local team-modality misuse with a UI validation message.
- [x] Add join button/action on individual BDT card/detail in `BdtPublishedGamesScreenController`.
- [x] Render joining/loading state per selected game to prevent duplicate taps during an in-flight request.
- [x] Render or navigate to a BDT waiting screen after success using the backend waiting-screen DTO.
- [x] Show backend validation errors clearly, especially duplicate registration and full capacity.
- [x] Keep mobile validation usability-only; backend remains authoritative for state, modality, capacity and duplicate checks.

## Mobile Tests

- [x] Add mobile API tests for endpoint URL, `POST` method, Authorization header and no request body.
- [x] Add mobile flow tests for successful individual join and waiting-screen DTO propagation.
- [x] Add mobile controller/render tests for join button visibility on individual games.
- [x] Add mobile controller/render tests proving team-modality games do not call the individual join endpoint.
- [x] Add mobile controller/render tests for loading state and duplicate-tap protection.
- [x] Add mobile tests for `401`, `403`, `404`, `409`, network and generic error messages.
- [x] Run mobile `npm test` and `npm run typecheck` after implementation.

## Acceptance, Traceability And Final Hardening

- [x] Update `acceptance.md` checklist with completed evidence for every acceptance criterion.
- [x] Replace pending evidence rows in `acceptance.md` with exact commands and pass counts.
- [x] Update `acceptance.md` with PostgreSQL concurrent capacity evidence and final pass counts.
- [x] Update `docs/04-sdd/traceability-matrix.md` with final 10/10 concurrency-hardening status, contract files and test evidence.
- [x] Update `docs/04-sdd/SPECS-LIST.md` to `10/10` only after the concurrent PostgreSQL capacity test and full suites pass.
- [x] Confirm HU-39 implementation and SDD reference only approved active services.
- [x] Confirm no participant gameplay was added to React web and no operator/admin behavior was added to mobile.
- [x] Confirm no RabbitMQ or SignalR implementation was introduced for HU-39 beyond the documented no-event/deferred-real-time decisions.
