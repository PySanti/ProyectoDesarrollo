# HU-01 — Tasks

## Domain

- [x] Define or confirm `Usuario` creation invariants for initial role assignment and active initial state.
- [x] Ensure domain model does not store passwords or sensitive credentials.
- [x] Confirm allowed `RolUsuario` values used during creation.

## Application

- [x] Create `CrearUsuarioConRolInicialCommand`.
- [x] Create command validator for `name`, `email`, and `initialRole`.
- [x] Implement `CrearUsuarioConRolInicialCommandHandler`.
- [x] Define `IUsuarioRepository`.
- [x] Define Keycloak integration port for user creation and role assignment.
- [x] Define response DTO/read model for created user.

## Infrastructure

- [x] Implement Keycloak adapter for user creation and initial role assignment.
- [x] Implement EF Core persistence for local `Usuario`.
- [x] Configure dependency injection for repository and Keycloak adapter.
- [x] Implement error translation for Keycloak and persistence failures.

## API

- [x] Expose `POST /api/identity/users`.
- [x] Wire endpoint/controller to MediatR command.
- [x] Apply administrator authorization.
- [x] Map business and integration errors to agreed HTTP responses.

## Contracts

- [x] Confirm HU-01 request/response details in `contracts/http/identity-api.md`.
- [x] Document an identity event only if explicitly approved during review.

## Tests

- [x] Add unit tests for validation and creation rules.
- [x] Add handler tests for success, duplicate email, unauthorized actor, and Keycloak failure.
- [x] Add integration tests for `POST /api/identity/users`.
- [x] Add contract tests for endpoint payloads.

## Frontend

- [x] Confirm React web admin form payload and error handling expectations against the HTTP contract.
- [x] Create React web app baseline under `frontend/`.
- [x] Integrate Keycloak login in frontend using `keycloak-js`.
- [x] Restrict HU-01 screen to users with `Administrador` role.
- [x] Implement create-user form (`name`, `email`, `initialRole`) and call `POST /api/identity/users`.
- [x] Map API errors (`400`, `403`, `409`, `500`, `502`) to user-facing messages.
- [x] Add frontend tests for auth guard and create-user form behavior.
- [x] Run frontend tests and production build.

## Acceptance / traceability

- [x] Update `acceptance.md` with manual and automated evidence.
- [x] Update HU-01 row in `docs/04-sdd/traceability-matrix.md` after implementation planning/review.

## Finalization checklist by phase

### Phase 1 - Blockers (requires user input)

- [x] Confirm if `HU-01` closes as academic demo only or with real Keycloak integration.
- [x] Confirm whether `UsuarioCreado` remains within final `HU-01` scope.
- [x] Define initial password policy for users created in Keycloak.
- [x] Confirm PostgreSQL runtime connection strategy.
- [x] Provide Keycloak configuration:
  - [x] Base URL.
  - [x] Realm.
  - [x] Client ID.
  - [x] Client secret or service-account credentials.
  - [x] Exact role names: `Administrador`, `Operador`, `Participante`.
  - [x] Required actions policy (for example password reset).

### Phase 2 - Authentication and authorization

- [x] Configure production authentication scheme in `Identity Service`.
- [x] Configure real JWT/Keycloak validation (`issuer`, `audience`, role-claim mapping).
- [x] Replace test-only auth assumptions with real administrator authorization.
- [x] Verify `403` for authenticated non-admin users under real auth.

### Phase 3 - Real Keycloak integration

- [x] Replace current `KeycloakIdentityAdapter` stub.
- [x] Implement real Keycloak user creation.
- [x] Implement real initial-role assignment in Keycloak.
- [x] Map Keycloak integration failures to application exceptions.
- [x] Ensure no local persistence on Keycloak failure (create user or assign role).

### Phase 4 - Persistence and runtime configuration

- [x] Configure PostgreSQL as runtime provider for normal execution.
- [x] Keep InMemory provider only for controlled local tests where appropriate.
- [x] Configure required settings (connection strings and Keycloak settings).
- [x] Verify unique-email behavior against real persistence.

### Phase 5 - API and contracts

- [x] Verify endpoint behavior matches `contracts/http/identity-api.md`.
- [x] Confirm final error mappings: `400`, `403`, `409`, `502`, `500`.
- [x] Confirm final create-user response shape.
- [x] Reconcile `design.md` with event-contract decision (`UsuarioCreado` kept or removed).

### Phase 6 - Tests

- [x] Add adapter tests for real Keycloak integration or controlled test double.
- [x] Add PostgreSQL integration tests.
- [x] Add real-auth integration tests for admin and non-admin flows.
- [x] Add failure-path tests for Keycloak create-user and role-assignment errors.
- [x] Re-run unit, integration and contract tests after real integration changes.
- [x] Generate coverage evidence.

### Phase 7 - Closure

- [x] Update `acceptance.md` with real Keycloak/PostgreSQL evidence.
- [x] Update `design.md` to reflect final auth and event decisions.
- [x] Update `traceability-matrix.md` only after end-to-end completion.
- [x] Mark `HU-01` as fully completed only when integration, tests and evidence are finished.
- [x] Add manual runtime evidence for React web login + create-user flow using real Keycloak (`Administrador`) credentials.

## Current blocker in this session

- [x] Execute runtime validation commands in an environment with .NET SDK installed (`dotnet` now available in this OpenCode runtime).

## Phase 8 - Technical closure gaps found in review

- [x] Add backend integration test for `409` duplicate email at `POST /api/identity/users`.
- [x] Add backend integration test for `502` Keycloak integration failure at `POST /api/identity/users`.
- [x] Add backend integration test that verifies no local persistence remains after Keycloak failure.
- [x] Add automated auth-pipeline coverage for administrator vs non-administrator access aligned with runtime JWT configuration.
- [x] Re-run unit, integration and contract tests after the new backend coverage is added.
- [x] Refresh coverage evidence after the new backend tests are added.
- [x] Decide whether `UsuarioCreado` remains within HU-01 scope.
- [x] If `UsuarioCreado` stays in scope, implement event publication and add tests. (N/A in HU-01: event moved out of scope)
- [x] If `UsuarioCreado` does not stay in scope, remove HU-01 design/traceability dependency on that event contract.
- [x] Update `acceptance.md` to separate automated evidence from manual/runtime verification.
- [x] Update `docs/04-sdd/traceability-matrix.md` status only after these gaps are resolved.

## Phase 9 - Welcome-email credential notification (extensión 2026-06-15)

### Application
- [x] Add `ITemporaryPasswordGenerator` port.
- [x] Add `IUserWelcomeEmailSender` port + `UserWelcomeEmailMessage` record.
- [x] Add `EmailDeliveryException`.
- [x] Extend `IKeycloakIdentityPort` with per-user password and `DeleteUserAsync`.
- [x] Add `IUsuarioRepository.RemoveAsync` for compensation.
- [x] Update handler: generate password, create in Keycloak, persist, send email, compensate on failure (no password in response, no password persisted).

### Infrastructure
- [x] Implement `CryptoTemporaryPasswordGenerator`.
- [x] Implement `SmtpUserWelcomeEmailSender` (System.Net.Mail) + `SmtpOptions` + branded `WelcomeEmailTemplate`.
- [x] Update `KeycloakIdentityAdapter` to use per-user password and add `DeleteUserAsync`; remove unused `TemporaryPassword` option.
- [x] Add `UsuarioRepository.RemoveAsync`.
- [x] Register new services + bind `Smtp` options with env fallback.

### API
- [x] Map `EmailDeliveryException` to `502`.

### Tests
- [x] Update all `IKeycloakIdentityPort` test doubles and register fake `IUserWelcomeEmailSender` in test factories.
- [x] Add handler tests: email sent on success, compensation on email failure.
- [x] Add `CryptoTemporaryPasswordGenerator` unit test (complexity + uniqueness).
- [x] Re-run unit/integration/contract tests (Unit 32, Contract 6, Integration 21 — all passing).

### Contracts / config / docs
- [x] Note welcome-email side effect + `502` mapping in `contracts/http/identity-api.md`.
- [x] Add SMTP env vars to `.env.example` and `GUIA-LEVANTAMIENTO.md`.
- [x] Update `acceptance.md` and `traceability-matrix.md`.
