# HU-34 - Tasks

## Domain

- [x] Add or verify `PartidaBDT` aggregate creation method for HU-34.
- [x] Add or verify `EtapaBDT` child entity with expected QR and time limit validation.
- [x] Add or verify modality limit validation for `Individual` and `Equipo`.
- [x] Add or verify `AreaBusqueda` textual value object validation.
- [x] Ensure BDT creation does not introduce numeric BDT score/ranking concepts.

## Application

- [x] Add `CrearPartidaBdtCommand`.
- [x] Add `CrearPartidaBdtCommandHandler`.
- [x] Add request/response DTOs or read models for created game summary.
- [x] Add application validation for request shape and modality-specific fields.

## Infrastructure

- [x] Add or verify EF Core mappings for BDT games and stages.
- [x] Add repository method for adding `PartidaBDT`.
- [x] Add Npgsql/PostgreSQL persistence coverage using isolated test database/schema.

## API

- [x] Add `POST /api/bdt/games` endpoint.
- [x] Enforce authenticated `Operador` authorization.
- [x] Map business validation failures to `400` or `409`.
- [x] Keep controller free of business rules.

## Contracts

- [x] Update `contracts/http/bdt-game-api.md` with `POST /api/bdt/games` after SDD review.
- [x] Update `contracts/events/bdt-game-events.md` to state no integration event is required for HU-34 closure.
- [x] Document that SignalR publication/lobby updates are deferred to HU-42 or HU-55 for HU-34 readiness.

## Tests

- [x] Add domain unit tests for creation invariants.
- [x] Add application handler tests.
- [x] Add API integration tests for success and authorization/error cases.
- [x] Add HTTP contract tests.
- [x] Add React web form tests for required fields and submit states.

## Frontend Web

- [x] Add operator BDT creation screen or route.
- [x] Add stages editor for one or more stages.
- [x] Add API client method for `POST /api/bdt/games`.
- [x] Render loading, success and error states.

## Acceptance and Traceability

- [x] Update `acceptance.md` with evidence after implementation.
- [x] Update `docs/04-sdd/traceability-matrix.md` after implementation status changes.
- [x] Update `docs/04-sdd/SPECS-LIST.md` after implementation status changes.

## Hardening to Reach 10/10

- [x] Reconcile the operator read-after-write verification path by removing it from HU-34 closure evidence; HU-34 closes through `POST /api/bdt/games`, while HU-37 keeps its own traceability for `GET /api/bdt/operator/games/published`.
- [x] Add backend validation and tests to reject duplicate stage `orden` values in the same BDT game so stage ordering is deterministic.
- [x] Add backend validation and tests for `etapas: null` to guarantee a clean `400` response instead of handler/domain exceptions.
- [x] Add domain/application tests for `Equipo` modality where `minimoParticipantes` exceeds `maximoEquipos`, and decide whether this should be rejected as a modality-limit conflict.
- [x] Add API integration tests for invalid stage payloads: empty `codigoQrEsperado`, non-positive `tiempoLimiteSegundos`, invalid `modalidad`, and invalid `modoInicio`.
- [x] Add contract tests for HU-34 error response shapes, not only status codes, covering `400`, `403`, `409`, and `500` mapping where feasible.
- [x] Extend PostgreSQL verification to assert all HU-34 persisted fields: `areaBusqueda`, `modalidad`, `minimoParticipantes`, `maximoParticipantes`, `maximoEquipos`, `minimoJugadoresPorEquipo`, `modoInicio`, stage `orden`, QR expected content, and time limit.
- [x] Add a frontend test for `Equipo` modality submission payload to verify `maximoParticipantes` is `null` and team limits are sent correctly.
- [x] Add a frontend test for missing `VITE_BDT_API_BASE_URL` or document the required environment variable in the HU-34 acceptance/setup evidence.
- [x] Record HU-34 smoke coverage: automated React web form tests plus API integration read-after-write with test `Operador`; real-browser Keycloak smoke remains environment-dependent and is documented in `acceptance.md`.
- [x] Run the full BDT backend test suite sequentially after hardening to avoid DLL-lock noise and record the final evidence in `acceptance.md`.
- [x] Run the full frontend test/build verification after hardening and record the final evidence in `acceptance.md`.

## Review Follow-up to Restore 10/10

- [x] Replace the single hard-coded `Etapa 1` form in React web with a dynamic stages editor that supports adding and removing multiple stages.
- [x] Ensure the React web payload sends all configured stages with deterministic positive `orden` values.
- [x] Add React web tests for adding at least two stages and submitting both in `etapas[]`.
- [x] Add React web tests for removing a stage and keeping valid stage order in the submitted payload.
- [x] Update `acceptance.md` after multi-stage UI is implemented and verified.
- [x] Update `docs/04-sdd/traceability-matrix.md` after the multi-stage UI and HU-37 traceability reconciliation are complete.
- [x] Update `docs/04-sdd/SPECS-LIST.md` only after HU-34 is truly back to `10/10`.
