# HU-44 - Tasks

## Planning Decisions for 10/10

- [x] Keep HU-44 owned by BDT Game Service.
- [x] Keep Team Service out of HU-44; participant eligibility is evaluated from BDT Game Service registration state.
- [x] Resolve no-active-stage behavior: return `409` for missing active stage instead of `200` with `puedeSubirTesoro=false`.
- [x] Use backend/server timestamps for countdown calculation.
- [x] Use the documented HU-43 `PartidaBDTIniciada` SignalR/WebSocket message for active-stage refresh.
- [x] Defer stage-closed/stage-advanced/cancelled real-time message names to their owning HUs unless contracts are approved before implementation.
- [x] Keep RabbitMQ out of HU-44 closure because this is a read-only participant query.

## Domain

- [x] Add or verify active-stage state in `EtapaBDT`.
- [x] Add or verify domain/read helpers to identify the active stage.
- [x] Add or verify participant registration access checks using BDT-owned `ExploradorBDT` state.
- [x] Ensure active-stage view does not mutate BDT state.

## Application

- [x] Add `ObtenerEtapaActivaBdtQuery`.
- [x] Add query validator for `PartidaId` and `ParticipanteUserId`.
- [x] Add query handler returning active-stage mobile DTO.
- [x] Map not-found and business conflicts to documented responses.
- [x] Map no-active-stage to `409` and never to a synthetic successful DTO.

## Infrastructure

- [x] Add repository/query method to read game, active stage and participant registration.
- [x] Add EF Core mappings for active-stage timer fields if missing.
- [x] Add Npgsql read-model coverage with isolated schema.

## API

- [x] Add `GET /api/bdt/games/{partidaId}/active-stage` endpoint.
- [x] Enforce authenticated `Participante` authorization.
- [x] Derive `ParticipanteUserId` from authenticated token claims only.
- [x] Return documented `400`, `401`, `403`, `404`, `409` and `500` cases.
- [x] Keep endpoint free of business rules.

## Contracts

- [x] Update `contracts/http/bdt-game-api.md` with HU-44 active-stage endpoint.
- [x] Update `contracts/events/bdt-game-events.md` with HU-44 read-only/no-RabbitMQ decision and approved real-time subscription behavior.
- [x] Document no-active-stage as `409` before implementation.
- [x] Keep contracts synchronized if implementation discovers a necessary shape change.

## Tests

- [x] Add application query tests for success and all rejection cases.
- [x] Add API integration tests for success and documented errors.
- [x] Add API integration test proving no-active-stage returns `409`.
- [x] Add HTTP contract tests.
- [x] Add PostgreSQL/Npgsql read tests.
- [x] Add mobile screen/controller tests.
- [x] Add mobile permission-denied tests for geolocation gate.
- [x] Add mobile real-time refresh test for documented `PartidaBDTIniciada` message.

## 10/10 Hardening Plan

- [x] Replace `navigator.geolocation` fallback with the project-approved React Native/Expo geolocation permission adapter.
- [x] Add tests for granted, denied and unavailable geolocation states using the real mobile permission adapter seam.
- [x] Add a ticking countdown state so `Tiempo restante` updates while the screen is mounted.
- [x] Add fake-timer tests proving the countdown decreases and reaches the unavailable/expired display state when appropriate.
- [x] Wire `BdtActiveStageScreenContainer` upload action to the documented HU-45 route or an explicit placeholder navigation contract if HU-45 screen is not implemented yet.
- [x] Add an integration-level mobile/container test proving pressing `Subir tesoro` in the real registered screen invokes navigation or the approved HU-45 handoff.
- [x] Keep team-modality active-stage access explicitly deferred to HU-40, or document and test the Team Service-backed mapping before enabling team BDT active-stage access.
- [x] Re-run backend unit, integration, contract, mobile tests and mobile typecheck after hardening.
- [x] Update `acceptance.md`, `SPECS-LIST.md` and `traceability-matrix.md` only after the hardening evidence passes.

## 10/10 Verification Target

- [x] Application tests cover success, missing game, unregistered participant, non-initiated game, no active stage and read-only behavior.
- [x] API integration tests cover documented `200`, `400`, `401`, `403`, `404`, `409` and non-mutation behavior.
- [x] Contract tests verify the HU-44 HTTP response and error contract.
- [x] PostgreSQL tests verify active-stage read model and participant-registration filtering with isolated schema.
- [x] Mobile tests verify loading, active-stage render, live ticking countdown from backend timestamps, upload action gating, real geolocation permission adapter states, no-active-stage conflict and navigation/handoff to HU-45 from the integrated container.
- [x] Mobile typecheck passes.
- [x] Real-time tests verify refresh on `PartidaBDTIniciada` and no subscription to undocumented event names.
- [x] Acceptance evidence records exact commands and passing counts before the HU is marked 10/10.

## Mobile

- [x] Add mobile API client for active-stage endpoint.
- [x] Add active-stage screen/controller under BDT feature.
- [x] Render stage order, countdown and upload action.
- [x] Request/check geolocation permission through a React Native-compatible permission adapter before enabling active participation.
- [x] Show clear permission-denied, loading, error and closed-stage states.
- [x] Show no-active-stage `409` as an unavailable-stage state without upload action.
- [x] Navigate to HU-45 upload flow, or an approved HU-45 handoff placeholder, from the integrated screen upload action.
- [x] Keep mobile validation usability-only.

## Acceptance and Traceability

- [x] Update `acceptance.md` with executed evidence after implementation.
- [x] Update `docs/04-sdd/traceability-matrix.md` after implementation status changes.
- [x] Update `docs/04-sdd/SPECS-LIST.md` after implementation status changes.
