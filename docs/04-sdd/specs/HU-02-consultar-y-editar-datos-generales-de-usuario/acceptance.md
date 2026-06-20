# HU-02 — Acceptance

## Acceptance checklist

- [x] HU-02 is assigned to `Identity Service`.
- [x] HU-02 scope includes user list, user detail, general data update, and user deactivation.
- [x] Only `Administrador` can execute HU-02 operations.
- [x] Update operation allows only general data fields (`name`, `email`).
- [x] Role modification is not allowed through HU-02 endpoints.
- [x] Duplicate email on update is rejected with business conflict (`409`).
- [x] Detail/update/deactivate over unknown user returns `404`.
- [x] Deactivation changes user status to `Desactivado`.
- [x] No password or sensitive credential is stored in UMBRAL.
- [x] Required automated tests exist (unit, application, integration, contract, frontend).
- [x] HTTP contract for HU-02 is updated in `contracts/http/identity-api.md`.
- [x] Traceability row for HU-02 is updated.

## Manual verification steps

1. Authenticate as administrator in React web.
2. Open HU-02 user management view.
3. Request user list and verify it returns existing users.
4. Request one user detail by `userId` and verify data consistency.
5. Update user `name` and `email` with valid values; verify success response and persisted change.
6. Attempt update using an email that belongs to another user; verify `409` conflict.
7. Attempt any HU-02 endpoint as non-admin and verify `403`.
8. Deactivate one existing user and verify status becomes `Desactivado`.
9. Re-check detail/list and verify deactivated state is visible.
10. Verify no password/sensitive credential is exposed in responses or local persistence model.

## Automated test evidence

- Unit tests:
  - Paths:
    - `services/identity-service/tests/Umbral.IdentityService.UnitTests/Hu02UsuarioDomainTests.cs`
    - `services/identity-service/tests/Umbral.IdentityService.UnitTests/Hu02ValidatorsTests.cs`
    - `services/identity-service/tests/Umbral.IdentityService.UnitTests/Hu02HandlersTests.cs`
  - Executed command:
    - `dotnet test services/identity-service/Umbral.IdentityService.sln`
  - Result summary:
    - `Umbral.IdentityService.UnitTests`: 27 passed, 0 failed.

- Application/handler tests:
  - Paths:
    - `services/identity-service/tests/Umbral.IdentityService.UnitTests/Hu02HandlersTests.cs`
  - Executed command:
    - `dotnet test services/identity-service/Umbral.IdentityService.sln`
  - Result summary:
    - Success and failure paths covered for `GetUsersQueryHandler`, `GetUserByIdQueryHandler`, `UpdateUserGeneralDataCommandHandler`, `DeactivateUserCommandHandler`.

- Integration tests:
  - Paths:
    - `services/identity-service/tests/Umbral.IdentityService.IntegrationTests/Hu02EndpointsIntegrationTests.cs`
  - Executed command:
    - `dotnet test services/identity-service/Umbral.IdentityService.sln`
  - Result summary:
    - `Umbral.IdentityService.IntegrationTests`: 21 passed, 0 failed.
    - Includes HU-02 auth (`401`/`403`), `404` not found, `409` duplicate email, and happy paths for list/detail/update/deactivate.

- Contract tests:
  - Paths:
    - `services/identity-service/tests/Umbral.IdentityService.ContractTests/Hu02ContractTests.cs`
  - Executed command:
    - `dotnet test services/identity-service/Umbral.IdentityService.sln`
  - Result summary:
    - `Umbral.IdentityService.ContractTests`: 6 passed, 0 failed.
    - Status code + response shape validated for all HU-02 endpoints.

- Frontend tests/build:
  - Paths:
    - `frontend/src/features/identity/UserManagementPage.tsx`
    - `frontend/src/features/identity/UserManagementPage.test.tsx`
    - `frontend/src/app/App.test.tsx`
    - `frontend/src/api/identityApi.ts`
  - Executed command:
    - `npm test` (workdir: `frontend`)
  - Result summary:
    - `3` test files passed, `11` tests passed, `0` failed.
    - Covers admin guard, HU-02 happy path (list/detail/update/deactivate) and mapped API errors (`403`, `404`, `409`, `500`).

- Backend runtime verification:
  - Executed command:
    - `dotnet run --project services/identity-service/src/Umbral.IdentityService.Api/Umbral.IdentityService.Api.csproj`
  - Result summary:
    - API starts and listens on `http://localhost:5000` without forcing DB connection during startup.
    - Startup no longer crashes due to eager `EnsureCreatedAsync` connection/auth check.

- Operational regression tests:
  - Executed command:
    - `dotnet test services/identity-service/Umbral.IdentityService.sln`
  - Result summary:
    - `Umbral.IdentityService.UnitTests`: 27 passed, 0 failed.
    - `Umbral.IdentityService.IntegrationTests`: 21 passed, 0 failed.
    - `Umbral.IdentityService.ContractTests`: 6 passed, 0 failed.

## Review evidence (inactive services and vocabulary)

- Verified implementation artifacts for HU-02 do not reference inactive services:
  - Audit Service
  - Scoring Service
  - Trivia Service
  - Treasure Hunt Service
  - Notification Service
- Verified HU-02 implementation artifacts do not use generic mission/session/evidence vocabulary and remain in Identity/User vocabulary.

## Traceability status

- HU-02 row exists in `docs/04-sdd/traceability-matrix.md`.
- HU-02 row is aligned for current stage (`Completed / tested / acceptance updated`).
- Contract alignment for HU-02 is documented in `contracts/http/identity-api.md` and re-validated through HU-02 contract tests.

## Frontend redesign update (2026-06-13)

Cambios de **presentación** durante la reconstrucción visual (ver
`docs/02-project-context/design/frontend-redesign-plan.md`, observaciones OBS-01 / OBS-02). No
alteran el contrato HTTP, las reglas de negocio ni los endpoints de HU-02.

- El panel de gestión **ya no muestra el rol/permisos** del usuario (se removieron el campo "Rol" y
  el input de rol solo-lectura del detalle). El rol se asigna en la creación (HU-01) y sigue sin ser
  modificable; aquí simplemente no se expone.
- La lista de usuarios se renderiza como **tabla con paginación** (columnas Nombre/Correo/Estado con
  indicador de estado, paginación cliente y empty state) en vez de una lista simple.
- `getIdentityUsers` sigue devolviendo `role` en el contrato; el frontend solo deja de mostrarlo.
- Pruebas frontend actualizadas: `frontend/src/features/identity/UserManagementPage.test.tsx` y
  `frontend/src/app/App.test.tsx` siguen verdes con la nueva UI.

## Assumptions

- HU-02 includes deactivation by explicit decision recorded during SDD creation for this session.

## Extensión 2026-06-15 — Reenvío de credenciales al cambiar el correo

Acceptance checklist (extensión):

- [x] Al cambiar el correo de un usuario con contraseña temporal pendiente, se le envía un correo con una nueva contraseña temporal (plantilla de marca) al nuevo correo.
- [x] El correo se sincroniza en Keycloak (atributo `email`) y se resetea una nueva contraseña temporal (`temporary=true`); la original nunca se persistió (RB-U03).
- [x] Si el correo no cambia, o el usuario ya completó su primer inicio de sesión (sin `UPDATE_PASSWORD`), la edición no envía correo ni toca Keycloak.
- [x] Si el reenvío falla, la edición devuelve `502` y revierte el cambio (email local + email en Keycloak al valor previo).
- [x] La contraseña no aparece en la respuesta HTTP.

Automated evidence (extensión):

- Handler: `..._Should_Resend_Credentials_When_Email_Changes_And_Temp_Password_Pending`, `..._Should_Not_Resend_When_Temp_Password_Not_Pending`, `..._Should_Not_Check_Keycloak_When_Email_Unchanged`, `..._Should_Revert_When_Email_Delivery_Fails` (`tests/.../Hu02HandlersTests.cs`).
- Suite tras la extensión: **Unit 36/36, Contract 6/6, Integration 21/21** (`dotnet test "services/identity-service/Umbral.IdentityService.sln"`).

Code references (extensión):

- `Application/Users/UpdateUserGeneralData/UpdateUserGeneralDataCommandHandler.cs`
- `Application/Abstractions/Identity/IKeycloakIdentityPort.cs` (+`HasTemporaryPasswordAsync`/`UpdateEmailAsync`/`ResetTemporaryPasswordAsync`)
- `Infrastructure/Identity/KeycloakIdentityAdapter.cs`
- `Api/Program.cs` (PATCH → `502` para Keycloak/email)

Pending manual evidence (runtime):

- [ ] Con SMTP real configurado: crear un usuario, cambiar su correo antes del primer login y verificar la recepción del correo con la nueva contraseña temporal en el nuevo correo.
