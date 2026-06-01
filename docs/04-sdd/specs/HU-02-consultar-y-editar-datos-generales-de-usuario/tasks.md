# HU-02 — Tasks

## Task status key

- [ ] Pending
- [x] Done

## Domain

- [x] Add behavior in `Usuario` aggregate for `EditarDatosGenerales(name, email)`.
- [x] Add behavior in `Usuario` aggregate for `Desactivar()`.
- [x] Ensure role immutability rule is preserved in domain behavior.

## Application

- [x] Create `GetUsersQuery` + handler.
- [x] Create `GetUserByIdQuery` + handler.
- [x] Create `UpdateUserGeneralDataCommand` + validator + handler.
- [x] Create `DeactivateUserCommand` + validator + handler.
- [x] Add/extend application exceptions for not found conflicts where needed.
- [x] Add read models/response DTOs for list/detail/update/deactivate flows.

## Infrastructure

- [x] Extend `IUsuarioRepository` contract for list/detail/update needs.
- [x] Implement repository methods in EF Core (`UsuarioRepository`).
- [x] Add duplicate email check excluding same user on update path.
- [x] Keep persistence error mapping consistent with existing exception strategy.

## API

- [x] Add `GET /api/identity/users` endpoint with `AdminOnly` authorization.
- [x] Add `GET /api/identity/users/{userId}` endpoint with `AdminOnly` authorization.
- [x] Add `PATCH /api/identity/users/{userId}` endpoint with `AdminOnly` authorization.
- [x] Add `PATCH /api/identity/users/{userId}/deactivation` endpoint with `AdminOnly` authorization.
- [x] Map errors to HTTP status codes (`400`, `401`, `403`, `404`, `409`, `500`).

## Review follow-up (feature review)

- [x] Implement all HU-02 API endpoints in `Umbral.IdentityService.Api/Program.cs` (currently only HU-01 endpoint exists).
- [x] Ensure `AdminOnly` policy is applied to every HU-02 endpoint.
- [x] Ensure endpoint-to-handler wiring uses CQRS/MediatR commands and queries defined in Application.
- [x] Verify HTTP status mapping for HU-02 runtime exceptions (`UserNotFoundException`, `DuplicateEmailException`, validation and persistence errors).

## Contracts

- [x] Finalize HU-02 sections in `contracts/http/identity-api.md` with request/response/error payloads.
- [x] Keep `contracts/events/identity-events.md` unchanged unless event scope is explicitly added to HU-02.

## Contract review alignment

- [x] Re-validate implemented API payloads/status codes against `contracts/http/identity-api.md` after API implementation.
- [x] Add HU-02 contract tests to prove runtime response shape and status codes match `contracts/http/identity-api.md`.

## Tests

- [x] Add/extend unit tests for domain methods in `Usuario`.
- [x] Add unit tests for validators.
- [x] Add handler tests for success and failure paths.
- [x] Add integration tests for HU-02 endpoints and auth scenarios.
- [x] Add contract tests for HU-02 HTTP contracts.

## Review blockers

- [x] Add objective test evidence (unit/application/integration/contract/frontend) before closing HU-02.
- [x] Complete acceptance evidence placeholders in `acceptance.md` with executed test commands and result summaries.

## Review-driven test closure

- [x] Add explicit unit tests for `Usuario.EditarDatosGenerales` and `Usuario.Desactivar` domain behavior.
- [x] Add handler tests for `GetUsersQueryHandler`, `GetUserByIdQueryHandler`, `UpdateUserGeneralDataCommandHandler`, and `DeactivateUserCommandHandler`.
- [x] Add integration tests for `401` unauthenticated and `403` non-admin cases in all HU-02 endpoints.
- [x] Add integration tests for `404` not found in detail/update/deactivate and `409` duplicate email in update.
- [x] Add contract tests validating response shape for list/detail/update/deactivate endpoints.

## Frontend (React web)

- [x] Add admin screen for user list and detail.
- [x] Add edit general data form.
- [x] Add deactivate action and state feedback.
- [x] Add API client methods for HU-02 endpoints.
- [x] Add frontend tests for guard, happy paths, and mapped error messages.

## Acceptance and traceability

- [x] Update `docs/04-sdd/specs/HU-02-consultar-y-editar-datos-generales-de-usuario/acceptance.md` with final evidence.
- [x] Update HU-02 row in `docs/04-sdd/traceability-matrix.md` (requirements, supporting services, contracts, status).
- [x] Align HU-02 status in `docs/04-sdd/SPECS-LIST.md` with current implementation progress.
- [x] Align HU-02 traceability text in `acceptance.md` with current matrix status.
- [x] Confirm DoR/DoD compliance before marking HU-02 as Completed.

## Review closure criteria

- [x] Mark this feature as `Completed` only after API + tests + acceptance evidence are complete.
- [x] Confirm no references to inactive services and no mission/session/evidence generic vocabulary in implementation artifacts.

## Operational hardening follow-up

- [x] Remove forced startup DB connection (`EnsureCreatedAsync`) from API bootstrap to avoid service crash on transient/local DB auth mismatch.
- [x] Align HU-02 HTTP contract response/error sections with implemented endpoints (`PATCH /users/{userId}`, `PATCH /users/{userId}/deactivation`).
- [x] Re-run backend and frontend automated tests after operational hardening changes.
