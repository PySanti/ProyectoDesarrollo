# SP-0 — Foundation (Gateway YARP + Service Shells + Target DBs) — Design

**Date:** 2026-06-23
**Slice:** SP-0 (Foundation) of the four-service code-structure migration
**Status:** Approved (design) — ready for implementation-plan writing
**Authoritative inputs:** `CLAUDE.md`; `docs/superpowers/specs/2026-06-22-code-structure-doctrine-migration-design.md` (decomposition table, Global Constraints, R1 gate); `services/identity-service` (canonical graded reference, post SP-1R).

## 1. Purpose & Scope

SP-0 stands up the **infrastructure of the target four-service topology** so later slices have somewhere to land. It is its own spec → plan → implementation cycle and leaves the system compiling, running, and green.

The migration moves the backend **from** the old layout (Identity / Team / Trivia Game / BDT Game, no gateway, where a "partida" was a single game) **to** the doctrine target: four services (**Identity, Partidas, Operaciones de Sesión, Puntuaciones**) behind a mandatory **YARP gateway**, with the `Partida → 1..* Juego` model. SP-0 builds the empty frame; SP-2 fills Partidas.

### Included
- A doctrine-complete **YARP gateway** as the single entry point (config-first; minimal, fail-secure code).
- Three new **graded + runnable + DbContext** service shells: `partidas`, `operaciones-sesion`, `puntuaciones`.
- Three new **target databases** + `docker-compose` entries + `run-local` scripts + `.env.example` per shell and for the gateway.
- An **ADR** fixing slugs / namespaces / ports (the "migration ADR" `CLAUDE.md` references).

### Excluded (deferred to SP-2+)
- Domain entities, real commands/queries, business HTTP/event contracts.
- Migrating runtime (SP-3) or scoring (SP-4) out of the legacy `trivia-game-service` / `bdt-game-service`.
- Renaming the existing `identity-service` (and the legacy game services).
- Repointing web/mobile clients to the gateway (SP-5).

## 2. Naming, Namespaces & Ports (→ ADR-0009)

The repo has a real inconsistency: on-disk folders use a `-service` suffix (`identity-service`), but `CLAUDE.md` (`run-local` examples and commands) uses suffix-less slugs (`services/partidas`, …). **Decision:** new shells use the doctrine (suffix-less) slugs; the existing `identity-service` and legacy game services are **not touched**. The temporary coexistence of `identity-service` and `partidas` is accepted migration debt, recorded in the ADR.

| Component | Folder | Root namespace | Local port | Database |
|---|---|---|---|---|
| Gateway | `gateway/` | `Umbral.Gateway` | **5080** (single entry point) | — |
| Identity *(exists, untouched)* | `services/identity-service` | `Umbral.IdentityService` | 5000 | `umbral_identity` |
| **Partidas** *(new)* | `services/partidas` | `Umbral.Partidas.*` | **5010** | `umbral_partidas` |
| **Operaciones de Sesión** *(new)* | `services/operaciones-sesion` | `Umbral.OperacionesSesion.*` | **5020** | `umbral_operaciones_sesion` |
| **Puntuaciones** *(new)* | `services/puntuaciones` | `Umbral.Puntuaciones.*` | **5030** | `umbral_puntuaciones` |

Ports avoid the occupied ones (identity 5000, trivia 5015, bdt 5016, identity compose host 5001). Services consumed by mobile must listen on `0.0.0.0` (per `CLAUDE.md`); the gateway is the host-published entry point.

## 3. Service Shell Anatomy (identical ×3; Identity = canonical reference)

Each `services/<svc>/` replicates the post-SP-1R graded structure of Identity. Project names use the per-service namespace root, e.g. `Umbral.Partidas.Domain`.

```
src/
  Umbral.<Svc>.Domain/            → empty but referenced (entities arrive in SP-2+)
  Umbral.<Svc>.Application/        → the exact graded folder set, empty:
        Commands/ Queries/ Interfaces/ Validators/ DTOs/
        Handlers/ Handlers/Commands/ Handlers/Queries/ Exceptions/
        + DependencyInjection.cs (AddApplication: MediatR + FluentValidation assembly scan)
  Umbral.<Svc>.Infrastructure/
        Persistence/  → <Svc>DbContext (points at its target DB, zero entities)
                        + DependencyInjection.cs (AddInfrastructure: AddDbContext)
        Services/     → empty (infra adapters arrive in SP-2+)
  Umbral.<Svc>.Api/
        Controllers/HealthController.cs   → inherits ControllerBase, GET /health → 200
        Middleware/ExceptionHandlingMiddleware.cs → centralized, mirrors Identity's
        Program.cs (slim): AddControllers, AddApplication, AddInfrastructure,
                           UseMiddleware<ExceptionHandlingMiddleware>, MapControllers,
                           `public partial class Program {}`
tests/
  Umbral.<Svc>.UnitTests/         → HealthControllerTests (graded: every controller has a unit test)
  Umbral.<Svc>.IntegrationTests/  → WebApplicationFactory<Program>: GET /health = 200
  Umbral.<Svc>.ContractTests/     → project present, minimal (filled in SP-2+)
Umbral.<Svc>.sln, run-local.sh, run-local.ps1, .env.example, service-context.md
```

**DbContext without entities:** wired to the target DB via `AddDbContext`, with an **EF InMemory fallback** when no connection string is set — mirroring the canonical `identity-service` Infrastructure DI. This proves the EF/DI wiring while nothing is modelled yet. SP-0's automated tests run against the InMemory fallback, so they need **no live Postgres** (this is exactly how `identity-service` tests run); real connection-string/connectivity is exercised manually via `run-local` in SP-0 and fully covered in SP-2 when entities land. No migrations are generated in SP-0 (no entities to migrate); the first migration lands in SP-2.

This satisfies the **R1 structural checklist** per shell: `Api/Controllers/` present; `Program.cs` has no `app.Map{Get,Post,Put,Delete,Patch}` (only `MapControllers`); `Application/` has exactly the mandated folder set; `Infrastructure/` has `Persistence/` + `Services/`; centralized exception middleware registered; every controller has a unit test.

## 4. Gateway YARP — config-first, minimal code, fail-secure

**Driving requirement (user):** push the maximum of the gateway's configuration into the web project's config (`appsettings`), keeping the **least possible gateway code** — without breaking good practice.

### What lives 100% in config (`appsettings.json` / `appsettings.{Environment}.json` / env)
Routes, clusters, destinations, transforms, the **route→role mapping** (`AuthorizationPolicy` per route), and Keycloak parameters. WebSocket passthrough is automatic in YARP. Example:

```jsonc
"ReverseProxy": {
  "Routes": {
    "identity":  { "ClusterId": "identity",  "Match": { "Path": "/identity/{**c}" },          "AuthorizationPolicy": "Default" },
    "partidas":  { "ClusterId": "partidas",  "Match": { "Path": "/partidas/{**c}" },          "AuthorizationPolicy": "Operador" },
    "operaciones": { "ClusterId": "operaciones", "Match": { "Path": "/operaciones-sesion/{**c}" }, "AuthorizationPolicy": "Default" },
    "puntuaciones": { "ClusterId": "puntuaciones", "Match": { "Path": "/puntuaciones/{**c}" },  "AuthorizationPolicy": "Default" }
  },
  "Clusters": {
    "identity":     { "Destinations": { "d1": { "Address": "http://identity-service:8080/" } } },
    "partidas":     { "Destinations": { "d1": { "Address": "http://partidas:8080/" } } },
    "operaciones":  { "Destinations": { "d1": { "Address": "http://operaciones-sesion:8080/" } } },
    "puntuaciones": { "Destinations": { "d1": { "Address": "http://puntuaciones:8080/" } } }
  }
}
```

### The irreducible code floor (~30 lines, `Program.cs`)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Routing: entirely from config
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// AuthN: registration is code; all values come from config/env
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", o => builder.Configuration.GetSection("Keycloak").Bind(o));

// AuthZ: the 3 base roles (doctrine: never change) + secure-by-default fallback
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Administrador", p => p.RequireRole("Administrador"))
    .AddPolicy("Operador",      p => p.RequireRole("Operador"))
    .AddPolicy("Participante",  p => p.RequireRole("Participante"))
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok()).AllowAnonymous();  // gateway's own healthcheck
app.MapReverseProxy();                                        // WebSockets passthrough automatic
app.Run();
public partial class Program {}                               // for WebApplicationFactory tests
```

### Why this split is the good-practice sweet spot (not a violation)
- **Routing/mapping in config IS the recommended YARP pattern** — topology changes without recompiling, ops-friendly, and keeps domain logic out of the gateway (doctrine: the gateway owns no domain logic).
- **The security border stays in code, fixed and fail-secure:**
  - `SetFallbackPolicy(RequireAuthenticatedUser)` makes any route lacking an explicit `AuthorizationPolicy` still require a token. In YARP a route without a policy is **anonymous by default** (fail-open); the fallback flips that to **secure by default**. This line must be code.
  - The JWT scheme registration and the 3 role policies are code so they are reviewable in PRs and unit-testable; pure-config auth is neither.
  - No flexibility is lost: doctrine fixes exactly three roles ("no new roles are ever created"), so the policies never change.
- **Coarse, route-level authorization by base role stays at the gateway; fine-grained functional-permission authorization stays inside each microservice** (per `CLAUDE.md`).

### Gateway specifics
- **Routes:** `/identity/*`→5000 (real), `/partidas/*`→5010, `/operaciones-sesion/*`→5020, `/puntuaciones/*`→5030 (shells expose only `/health` for now).
- **Health:** the gateway's own `/health` is a one-line minimal-API endpoint, `AllowAnonymous`. The graded `Controllers/` mandate targets the four domain services, not the proxy; a minimal endpoint is idiomatic for a routing host and keeps gateway code minimal.
- **AuthN values** (`Authority`/`Audiences`/`Issuer`/metadata) come from env (`KEYCLOAK_VALID_AUDIENCES` / `KEYCLOAK_VALID_ISSUERS` / authority URL), mirroring the services' defense-in-depth; configurable so local dev can run without a live Keycloak.
- **WebSockets:** passthrough enabled (needed for SP-3 SignalR).
- **Legacy `trivia-game-service` / `bdt-game-service` are NOT routed** through the gateway; clients keep hitting them directly until SP-5.

```
gateway/
  src/Umbral.Gateway/
    Program.cs                 → the ~30-line floor above
    appsettings.json           → ReverseProxy (routes + clusters) + Keycloak section
    appsettings.Development.json
  tests/Umbral.Gateway.IntegrationTests/  → /health = 200; protected route without token = 401
  Umbral.Gateway.sln, run-local.sh, run-local.ps1, .env.example, gateway-context.md
```

## 5. Infra / DB / docker-compose

- `infra/docker-compose.yml`: add services `partidas`, `operaciones-sesion`, `puntuaciones`, and `gateway`; correct the "approved services" comment to the target topology (keeping `trivia-game-service` / `bdt-game-service` as legacy-in-transit). Ideally only the gateway publishes a host port; the four services stay on the internal network. Compose host-port scheme: gateway `5080:8080`; services optionally exposed for debugging (`5011`/`5021`/`5031`:8080).
- Create the target DBs `umbral_partidas`, `umbral_operaciones_sesion`, `umbral_puntuaciones` (already documented in `CLAUDE.md` / `GUIA-LEVANTAMIENTO.md`).
- `run-local.sh` / `run-local.ps1` + `.env.example` per shell and for the gateway, mirroring Identity (load `services/<svc>/.env`, bind `0.0.0.0`, set the local port).

## 6. Contracts & Docs

- `contracts/http/`: a `health.md` stub documenting the common `/health` contract. Business contracts for Partidas arrive in SP-2.
- **ADR-0009** in `docs/05-decisions/`: records the four-service slug/namespace/port convention, the gateway topology, `run-local`/`.sln` finalization, and the accepted `identity-service` vs. suffix-less coexistence. This is "the migration ADR" `CLAUDE.md` points at for finalized slugs/ports.
- Update `gateway/gateway-context.md` and add a `service-context.md` per new shell.

## 7. Testing & R1 Gate

- **Each shell:** `HealthController` unit test (graded) + integration test asserting `GET /health = 200` + a contract test project present (minimal).
- **Gateway:** integration test asserting `/health = 200` and a protected route without a token → `401` (verifies the fail-secure fallback and JWT pipeline).
- Every slice leaves build + tests green. On SP-0 close, run the **R1 structural gate** (multi-agent review) over the three shells + gateway against the structural checklist; defects route to a fix wave before SP-0 closes.

## 8. Decisions Log (this brainstorming)

1. **Foundation handling:** full SP-0 first as its own slice (gateway + 3 shells + DBs + compose), then SP-2 fills Partidas.
2. **Shell depth:** graded + runnable + DbContext (compiles, runs, `/health` 200, passes R1 structural).
3. **Naming:** doctrine suffix-less slugs for new shells (`partidas`, `operaciones-sesion`, `puntuaciones`), namespaces `Umbral.Partidas.*` / `Umbral.OperacionesSesion.*` / `Umbral.Puntuaciones.*`; existing `identity-service` and legacy game services untouched; recorded in ADR.
4. **Gateway:** doctrine-complete (JWT validation, route-level role authz, WS passthrough, anonymous `/health`), implemented **config-first** with a minimal fail-secure code floor (~30-line `Program.cs`); routing/mapping in `appsettings`, security border in code.

## 9. Risks & Mitigations

| Risk | Mitigation |
|---|---|
| Gateway config-first drifts into fail-open auth. | `SetFallbackPolicy(RequireAuthenticatedUser)` in code; integration test asserts 401 on a protected route without a token. |
| Mixed naming (`identity-service` vs `partidas`) confuses contributors. | ADR-0009 records the convention and the deferred rename explicitly. |
| Empty DbContext masks a broken connection string. | Acceptable for SP-0: the DbContext has zero entities, so there is nothing to persist yet; the EF/DI wiring is proven by an InMemory-backed integration test (mirroring `identity-service`), and real connection-string/connectivity is verified via `run-local` in SP-0 and fully covered in SP-2 when entities land. |
| Three near-identical shells invite copy-paste drift. | Identity is the single canonical reference; the plan builds shell #1 fully, then mirrors it; R1 gate checks all three. |
