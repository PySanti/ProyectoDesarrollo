# HU-01 — Acceptance

## Acceptance checklist

- [x] HU-01 is assigned to `Identity Service`.
- [x] The create-user endpoint is defined and aligned with the Identity contract.
- [x] Only administrators can create users.
- [x] Initial role is mandatory and valid.
- [x] Local user persistence includes `KeycloakId`.
- [x] No password or sensitive credential is stored in UMBRAL.
- [x] Duplicate email is rejected with a business conflict.
- [x] Keycloak integration failure is handled without inconsistent local state.
- [x] Required automated tests exist for domain/application/integration behavior.
- [x] React web frontend exists for HU-01 and authenticates with Keycloak.
- [x] HU-01 React web screen is restricted to authenticated `Administrador` role.
- [x] Frontend tests verify auth guard and create-user behaviors.
- [x] Frontend production build succeeds.
- [x] Traceability is updated.

## Manual verification steps

1. Authenticate as an administrator.
2. Submit a valid create-user request with name, email, and initial role.
3. Verify the response includes `userId`, `keycloakId`, `role`, and `status`.
4. Verify the local record exists and does not contain password data.
5. Repeat with a duplicated email and verify conflict behavior.
6. Repeat as a non-admin user and verify authorization failure.
7. Simulate Keycloak failure and verify integration error without partial persistence.

## Current evidence status

- [x] Domain/Application/Infrastructure implementation code exists for HU-01.
- [x] Domain model excludes password/sensitive credential fields (`services/identity-service/src/Umbral.IdentityService.Domain/Entities/Usuario.cs`).
- [x] Allowed role values for creation are implemented (`services/identity-service/src/Umbral.IdentityService.Domain/Enums/RolUsuario.cs`).
- [x] Command validator rules for `name`, `email`, and `initialRole` are implemented and tested (`services/identity-service/tests/Umbral.IdentityService.UnitTests/CreateUserValidatorAndDomainTests.cs`).
- [x] Command handler orchestration is implemented and tested (`services/identity-service/tests/Umbral.IdentityService.UnitTests/CreateUserHandlerTests.cs`).
- [x] Repository port and Keycloak port are implemented in executable code.
- [x] Response DTO/read model is implemented in executable code.
- [x] Keycloak adapter is implemented in executable code.
- [x] EF Core persistence for local `Usuario` is implemented in executable code.
- [x] Unit: command validator tests
- [x] Unit/Application: command handler tests
- [x] Integration: create user endpoint tests
- [x] Contract: identity create-user contract tests

Automated evidence summary (current):

- Unit tests: `7/7` passed.
- Integration tests: `5/5` passed.
- Contract tests: `2/2` passed.

## Runtime closure checklist

- [x] Validate real `POST /api/identity/users` execution with Keycloak admin token.
- [x] Validate `403` with authenticated non-admin token against real auth pipeline.
- [x] Validate duplicate email conflict (`409`) against real PostgreSQL persistence.
- [x] Validate Keycloak failure path (`502`) without partial local persistence.
- [x] Attach runtime evidence (request/response logs or screenshots) for the previous checks.
- [x] Re-run automated tests in local environment with .NET SDK available.

## Pending manual evidence (frontend runtime)

- [x] Authenticate in React web app through real Keycloak login as `Administrador`.
- [x] Execute HU-01 create-user from React UI against running Identity Service.
- [x] Capture evidence of successful UI flow (`201`) and duplicate-email conflict (`409`) from UI.

Automated test evidence:

- `dotnet test "services/identity-service/Umbral.IdentityService.sln"` executed successfully in local environment.
- Result summary: Unit `7/7`, Contract `2/2`, Integration `5/5` passed.
- Coverage generated with `dotnet test "services/identity-service/Umbral.IdentityService.sln" --collect:"XPlat Code Coverage"`.
- Coverage files:
  - `services/identity-service/tests/Umbral.IdentityService.UnitTests/TestResults/31ad12cb-ae7c-4849-a3da-213b5e1989f2/coverage.cobertura.xml`
  - `services/identity-service/tests/Umbral.IdentityService.IntegrationTests/TestResults/69ccb2e6-d5b6-40cb-bb0f-3c48977b358f/coverage.cobertura.xml`
  - `services/identity-service/tests/Umbral.IdentityService.ContractTests/TestResults/1b259f15-9a53-45ba-b3ed-04bdb7ea4fd3/coverage.cobertura.xml`

Runtime evidence:

- Runtime admin success (`201`):
  - Email: `hu01.final.463e8a1c@test.com`
  - Response includes `userId`, `keycloakId`, `name`, `email`, `role`, `status`.
- Runtime non-admin forbidden (`403`) validated in same request cycle.
- Runtime duplicate email conflict (`409`) validated in same request cycle.
- Controlled Keycloak failure (`502`) validated by rotating `identity-service` secret in Keycloak and restoring it.
  - Email: `hu01.502.0d39c2d1@test.com`
  - Response: `{"message":"Failed to get Keycloak token. StatusCode=401"}`.

## Frontend contract confirmation

- React web admin form payload confirmed against contract: `name`, `email`, `initialRole`.
- Error handling expectations confirmed against contract: `400`, `403`, `409`, `502`, `500`.
- Reference: `contracts/http/identity-api.md`.

Frontend implementation evidence:

- App scaffold and HU-01 page implemented under `frontend/`.
- Keycloak auth adapter implemented in `frontend/src/auth/keycloak.ts`.
- Admin create-user form implemented in `frontend/src/features/identity/CreateUserPage.tsx`.
- Frontend automated tests:
  - `frontend/src/app/App.test.tsx`
  - `frontend/src/features/identity/CreateUserPage.test.tsx`
- Frontend command evidence:
  - `npm run test` passed (`5` tests).
  - `npm run build` passed.

Frontend runtime evidence (real Keycloak + running Identity Service):

- Headless browser runtime checks executed with Playwright against `http://localhost:5173`.
- Login in Keycloak realm `UMBRAL-UCAB` with administrator credentials succeeded and HU-01 form became visible.
- UI create-user success (`201`) confirmed from frontend success message:
  - `Usuario creado: HU01 Runtime 54094801 (hu01.runtime.54094801@test.com) - rol Participante`
- UI duplicate-email conflict (`409`) confirmed from frontend error message:
  - `El correo ya existe.`
- Runtime scripts and evidence assets:
  - `frontend/scripts/hu01-runtime-check.mjs`
  - `frontend/scripts/hu01-runtime-create-user.mjs`
  - `frontend/runtime-hu01-after-login.png`
  - `frontend/runtime-hu01-create-user.png`

## Traceability status

- HU: `HU-01`
- Owning service: `Identity Service`
- Client target: `React web`
- Contract file:
  - `contracts/http/identity-api.md`
- Event contract:
  - Not required for HU-01 closure.
- Status:
  - `Completed with automated + runtime Keycloak/PostgreSQL verification evidence`
