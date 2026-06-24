# SP-1R — Identity Graded-Structure Conformance + Forward Enforcement Design

Date: 2026-06-23

## Purpose

Bring the `Identity` service into **full literal compliance** with CLAUDE.md's graded "Structure & coding rules", and amend the project doctrine so that **every remaining sub-project (SP-2..SP-5) is built compliant from the start**. This is a structural remediation of SP-1's output (which lifted `team-service` into Identity's pre-existing Minimal-API layout): it reshapes folders, controllers, and exception handling **without changing any HTTP contract, business rule, event, or observable behavior**.

The final SP-1 compliance review found Identity NON-COMPLIANT on four graded items (Minimal APIs / no `Controllers` / no `BaseController` / no controller unit tests; per-slice `Application/` instead of the mandated flat layout; no centralized exception handling; no `Infrastructure/services/`). All four are pre-existing, service-wide patterns; the SP-1 *plan* (line 17) softened the SP-1 *design*'s explicit requirement (design lines 109/119) to follow Identity's existing pattern. This sub-project closes that gap and makes the refactored Identity the **canonical reference** the other three services copy.

## Authoritative Inputs

- `CLAUDE.md` — "Structure & coding rules (graded)" (the rules being satisfied; also amended here, see Deliverable B).
- `docs/superpowers/specs/2026-06-22-code-structure-doctrine-migration-design.md` — the migration design whose Global Constraints are amended here.
- `services/trivia-game-service/` — closest existing reference (Controllers + `ExceptionHandlingMiddleware`); used as a pattern source, not copied wholesale.

## Confirmed Decisions (user-approved 2026-06-23)

1. **Compliance bar = full literal CLAUDE.md graded checklist** (not a pragmatic subset). All five graded areas are satisfied.
2. **`BaseController` = the native `Microsoft.AspNetCore.Mvc.ControllerBase`.** No custom base class is created; controllers inherit `ControllerBase` directly (as `trivia-game-service` already does) and reuse the existing `AuthenticatedUserClaims.TryGetUserId` helper for the `sub` claim. ("BaseController" in CLAUDE.md is read as the framework base controller; ASP.NET Core has no type literally named `BaseController` — the native ones are `ControllerBase` and `Controller`.)
3. **Scope = the whole Identity service** (Users + Teams/Invitations). The graded rule "`Program.cs` must not build/register controllers inline" admits no Minimal-API endpoint, so all 13 endpoints become controllers — the User endpoints (not part of SP-1) are included.
4. **`Application/` is strictly flat** (global CQRS folders, no per-feature slice folders).
5. **Forward enforcement = documentation/process only** (no automated structural-lint tooling): amend the migration design + CLAUDE.md, plus a manual structural checklist in the R1 gate.
6. **Response/DTO models live in a new `Application/DTOs/` folder**, and **CLAUDE.md is amended** to sanction `DTOs/` for all services. Application-layer exceptions live in `Application/Exceptions/` (the two current locations unified); CLAUDE.md is amended to sanction `Exceptions/` too, so it does not contradict its own "strictly these folders".
7. **3 controllers**, grouped by family: `UsersController`, `TeamsController`, `TeamInvitationsController`.

## Current State (Identity, on disk)

- **Api**: `Program.cs` (599 lines) maps all 13 endpoints as Minimal APIs with inline `try/catch` exception→status mapping; `Authentication/{AuthenticatedUserClaims,KeycloakRoleClaims}.cs`. No `Controllers/`, no middleware.
- **Application**: per-slice folders `Teams/{CreateTeam,LeaveTeam,TransferLeadership,Invitations/*,Queries/*,Exceptions}` and `Users/{CreateUser,DeactivateUser,GetUserById,GetUsers,UpdateUserGeneralData,Common}`; abstractions under `Abstractions/{Events,Identity,Notifications,Persistence,Security}`; `Exceptions/`.
- **Domain**: `Entities/`, `Enums/`, `Exceptions/`, `Abstractions/Persistence/` (holds `IEquipoRepository`, `IInvitacionEquipoRepository`). **Already compliant**, except `IUsuarioRepository` lives in Application, not Domain.
- **Infrastructure**: `Persistence/` (DbContext + 3 repos), plus `Events/`, `Identity/`, `Notifications/`, `Security/`. Has `Persistence/` but no `Services/`.
- **Tests**: `UnitTests`, `IntegrationTests`, `ContractTests` (165 total green). No controller unit tests.

## Target Structure

### Domain (one move)
Move `IUsuarioRepository` from `Application/Abstractions/Persistence/` → `Domain/Abstractions/Persistence/` (repository interfaces belong to Domain, per CLAUDE.md). Update namespace + references (`UsuarioRepository`, the user handlers, DI).

### Application (strictly flat)
```
Application/
  Commands/            9 command records (CreateUserWithInitialRole, DeactivateUser, UpdateUserGeneralData,
                       CrearEquipo, SalirDeEquipo, TransferirLiderazgo,
                       EnviarInvitacionEquipo, AceptarInvitacionEquipo, RechazarInvitacionEquipo)
  Queries/             4 query records (GetUsers, GetUserById, GetInvitacionesRecibidas, GetParticipantesElegibles)
  Interfaces/          IEquipoEventsPublisher, IKeycloakIdentityPort, IUserWelcomeEmailSender,
                       ITemporaryPasswordGenerator   (application ports; repository interfaces are in Domain)
  Validators/          9 XCommandValidator
  DTOs/                response models: CrearEquipoResponse, SalirDeEquipoResponse, TransferirLiderazgoResponse,
                       EnviarInvitacionEquipoResponse, AceptarInvitacionEquipoResponse, RechazarInvitacionEquipoResponse,
                       InvitacionRecibidasItemResponse, ParticipanteElegibleResponse,
                       CreateUserWithInitialRoleResponse, DeactivateUserResponse, UpdateUserGeneralDataResponse,
                       UserDetailResponse, UserSummaryResponse
  Handlers/
    Commands/          9 XCommandHandler
    Queries/           4 XQueryHandler
  Exceptions/          application-layer exceptions unified here (current Application/Exceptions/* + Teams/Exceptions/*):
                       DuplicateEmail, EmailDelivery, KeycloakIntegration, Persistence, UserNotFound,
                       AlreadyBelongsToActiveTeam, ConcurrentTeamCreation, InvitacionNoEncontrada,
                       InvitacionPendienteYaExiste, LeaveTeamConflict, NoActiveTeamForParticipant,
                       ParticipantAlreadyInTargetTeam, TeamFull, TransferirLiderazgoConflict, UniqueMembershipConflict
  DependencyInjection.cs
```
MediatR/FluentValidation auto-discovery is assembly-scan based, so flattening folders does not change registration. The `Abstractions/` sub-namespaces collapse into `Interfaces/`.

### Infrastructure (`Persistence/` + `Services/`)
```
Infrastructure/
  Persistence/   IdentityDbContext, UsuarioRepository, EquipoRepository, InvitacionEquipoRepository   (unchanged)
  Services/      Events/NoOpEquipoEventsPublisher
                 Identity/{KeycloakIdentityAdapter, KeycloakOptions}
                 Notifications/{SmtpUserWelcomeEmailSender, SmtpOptions, WelcomeEmailTemplate}
                 Security/CryptoTemporaryPasswordGenerator
  DependencyInjection.cs
```
PascalCase folders kept (repo + .NET convention; CLAUDE.md's lowercase is prose). Update namespaces + DI registrations.

### Api (Controllers + centralized exceptions)
```
Api/
  Controllers/
    UsersController.cs            [Authorize(Policy="AdminOnly")]    route /api/identity/users      (5 actions)
    TeamsController.cs            [Authorize(Policy="ParticipantOnly")] route /api/teams            (create, DELETE /membership, PATCH /leadership)
    TeamInvitationsController.cs  [Authorize(Policy="ParticipantOnly")] routes /api/teams/invitations + /api/teams/eligible-participants (send, inbox, accept, reject, eligible)
  Contracts/                      request records moved from Program.cs:
                                  CrearEquipoRequest, TransferirLiderazgoRequest, EnviarInvitacionRequest, UpdateUserGeneralDataRequest
  Middleware/
    ExceptionHandlingMiddleware.cs
  Utils/                          AuthenticatedUserClaims, KeycloakRoleClaims
                                  (renamed from the misleading Authentication/: these files do NOT
                                  authenticate — Keycloak + the JwtBearer middleware do. They only
                                  read/normalize claims from the already-validated token:
                                  KeycloakRoleClaims flattens Keycloak's nested realm_access/resource_access
                                  roles into .NET role claims so RequireRole/AdminOnly/ParticipantOnly work;
                                  AuthenticatedUserClaims extracts the `sub` actor id for controllers.)
  Program.cs                      slimmed
```

Each controller: `[ApiController]`, inherits `ControllerBase`, injects `IMediator` + the relevant `IValidator<T>`; per action — validate (→ `ValidationProblem`/400 on failure, same as today), resolve `ActorUserId` via `AuthenticatedUserClaims.TryGetUserId` (→ `Unauthorized`/401 if absent, team/invitation actions only), `await _mediator.Send(...)`, return `Created`/`Ok`. No `try/catch`: domain/application exceptions propagate to the middleware. No business logic.

**`ExceptionHandlingMiddleware`** preserves the exact current mapping (verified against `Program.cs`):

| Exception(s) | Status |
|---|---|
| `AlreadyBelongsToActiveTeamException`, `ConcurrentTeamCreationException`, `UsuarioYaEnEquipoException`, `LeaveTeamConflictException`, `TransferirLiderazgoConflictException`, `EquipoLlenoException`, `InvitacionPendienteYaExisteException`, `DuplicateEmailException` | 409 |
| `NoActiveTeamForParticipantException`, `InvitacionNoEncontradaException`, `UserNotFoundException` | 404 |
| `NoEsLiderException` | 403 |
| `KeycloakIntegrationException`, `EmailDeliveryException` | 502 |
| `PersistenceException` + unhandled fallback | 500 |

Response body stays `{ "message": "..." }` to match current output. `ValidationProblem` (400) and `Unauthorized` (401) remain controller-level (not exceptions), exactly as today.

`Program.cs` reduces to: builder config (Keycloak JWT, CORS, `AddIdentityApplication/Infrastructure`), `AddControllers()`, DB bootstrap block (unchanged), `UseMiddleware<ExceptionHandlingMiddleware>()`, `UseCors/UseAuthentication/UseAuthorization`, `MapControllers()`, `app.Run()`. Keep `public partial class Program {}` (required by `WebApplicationFactory<Program>`).

## Testing

- **New controller unit tests** (one class per controller — graded requirement): mock `IMediator`/`IValidator<T>`, assert each action dispatches the correct command/query and returns the correct `IActionResult` (`CreatedResult`/`OkObjectResult`/`ValidationProblem`/`UnauthorizedResult`). No business-rule assertions (covered by existing unit/integration tests).
- **New middleware unit test**: each mapped exception → expected status + `{message}` body; unhandled → 500.
- **Existing Contract + Integration suites pass unmodified.** Routes, status codes, and payloads are unchanged, so they validate controllers + middleware end-to-end and are the regression net proving behavior is preserved. (If a contract test references a Minimal-API-only artifact, that is a defect to fix in the refactor, not a test to weaken.)
- Identity suite green at every step (currently 165) plus the new controller/middleware tests.

## Migration Strategy

Layer by layer; each step is one coherent, independently-verifiable, green commit:

1. Domain — move `IUsuarioRepository` → green.
2. Application — flatten to global folders, add `DTOs/`, unify `Exceptions/` → green.
3. Infrastructure — introduce `Services/`, move non-persistence impls → green.
4. Api — add `ExceptionHandlingMiddleware` + 3 controllers + `Contracts/`, slim `Program.cs`, delete inline endpoints/try-catch → green (contract/integration still pass).
5. New controller + middleware unit tests → green.
6. Deliverable B (doc + CLAUDE.md amendments).

Rejected: *feature-by-feature* (leaves `Program.cs` half-controllers/half-minimal — incoherent); *big-bang* (one unreviewable, unbisectable commit).

## Deliverable B — Forward Enforcement (doc/process only)

1. **Amend `CLAUDE.md`** "Structure & coding rules (graded)" → `Application/`: change the mandated folder set to `Commands/`, `Queries/`, `Interfaces/`, `Validators/`, `Handlers/` (`Handlers/Commands/`, `Handlers/Queries/`), **`DTOs/`** (request/response models), and **`Exceptions/`** (application-layer exceptions). Note these are the exact top-level folders (no per-feature slice folders). This directive applies to all four services.
2. **Amend the migration design** (`2026-06-22-code-structure-doctrine-migration-design.md`) Global Constraints to (a) match the amended folder set and (b) state the graded structure is a **mandatory acceptance criterion for every SP**, with the refactored Identity as the canonical reference.
3. **Add a manual structural checklist to the R1 gate** definition: `Controllers/` present and `Program.cs` free of `app.Map{Get,Post,Put,Delete,Patch}`; `Application/` has exactly the mandated folders; `Infrastructure/` has `Persistence/` + `Services/`; centralized exception middleware present; every controller has a unit test.
4. Note that SP-2..SP-5 build on the canonical template from the start (near-zero cost — they are new services).

## Scope

**Included:** the four graded areas across the whole Identity service; the new `DTOs/` folder; the CLAUDE.md + migration-design amendments; the R1 structural checklist.

**Excluded:** any HTTP/event contract change; any business rule, validation, or status-code change; a MediatR `ValidationBehavior` pipeline (controllers keep explicit per-action validation to preserve behavior exactly — possible future cleanup, not here); restructuring `trivia-game-service`/`bdt-game-service` (they are reformed/absorbed in SP-2); automated structural-lint tooling (enforcement is doc/process per decision 5).

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| Refactor silently changes an HTTP response/status. | Exact mapping table preserved; existing Contract + Integration suites must pass unmodified. |
| Namespace churn (Application flatten, Infrastructure `Services/`, `IUsuarioRepository` move) breaks build. | Layer-by-layer, green per step; assembly-scan DI is folder-agnostic. |
| Amending CLAUDE.md introduces an internal contradiction. | `DTOs/` + `Exceptions/` explicitly sanctioned so the "exact folders" rule matches the actual layout. |
| Future SPs ignore the convention. | Mandatory acceptance criterion + manual R1 structural checklist; Identity stands as the copyable reference. |

## Approval Status

Decisions 1–7 approved by the user on 2026-06-23. Global design (Domain/Application/Infrastructure/Api/Testing/Strategy/Deliverable B) approved. Review refinement: `Api/Authentication/` renamed to `Api/Utils/` (the files only read/normalize claims; they do not authenticate — namespace `...Api.Authentication` → `...Api.Utils`). Written spec **reviewed and APPROVED by the user on 2026-06-23**. Next: implementation-plan authoring (writing-plans).
