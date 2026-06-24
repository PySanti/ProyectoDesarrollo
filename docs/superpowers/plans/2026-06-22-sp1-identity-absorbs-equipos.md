# SP-1 — Identity Absorbs Equipos Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fold the entire `team-service` into the `Identity` service (one service `Umbral.IdentityService`, one DB `umbral_identity`) and replace the obsolete `CodigoAcceso` join-by-code mechanism with the doctrine-mandated `InvitacionEquipo` (team invitations), keeping the still-valid team domain logic and adapting the mobile teams flow — without adding new HUs.

**Architecture:** Lift-and-reshape. The Equipos bounded context moves into Identity's existing Clean Architecture projects (`Domain`/`Application`/`Infrastructure`/`Api`) under `*.Teams` namespaces, persisting into `umbral_identity` via the existing `IdentityDbContext`. The access-code stack (generator, port, join-by-code slice, EF column/index, event field, mobile screens, HU-04 tests) is deleted; a new `InvitacionEquipo` aggregate + send/accept/reject commands + inbox/eligible-participants queries + repository + events + mobile invitations inbox replace it. Each task leaves the build and tests green.

**Tech Stack:** .NET 8, Clean Architecture + CQRS via MediatR, FluentValidation, EF Core + Npgsql (PostgreSQL `umbral_identity`), xUnit (Unit/Contract/Integration), Keycloak JWT, React Native/Expo (mobile, `node --test`).

## Global Constraints

- Documentation-and-code migration of EXISTING functionality; **no new HUs/features**. `InvitacionEquipo` replaces the access-code join rule (it is a rule change, not a new HU).
- Target service: `Identity` (`Umbral.IdentityService.*`), DB `umbral_identity`, single `IdentityDbContext`. The `team-service` tree, `Umbral.TeamService.*`, and DB `umbral_team` are removed once green.
- **There is no team access code.** Members join only via `InvitacionEquipo` sent by the leader, from a dynamic participant list that excludes anyone already in a team and is blocked when the team is full. Invitations do not expire; deleting a team deletes its pending invitations (history preserved). Team is 1–5 members; creator is first member and leader; a user belongs to only one active team at a time.
- `InvitacionEquipo` is distinct from `Convocatoria` (partida-level, owned by Operaciones de Sesion / SP-3). Do not touch `Convocatoria`.
- Standardized layout per service: `*.Domain` holds entities/enums/exceptions AND repository interfaces; `*.Application` holds `Commands/Queries/Interfaces/Validators/Handlers` (handlers under `Handlers/Commands` and `Handlers/Queries` where the project uses that split — otherwise follow Identity's existing per-slice folder pattern); `*.Infrastructure` holds `persistence/` and `services/`; controllers/endpoints contain no business logic and dispatch via MediatR.
- Team/invitation endpoints are authorized `ParticipantOnly` (role `Participante`), NOT `AdminOnly`. Identity must validate the `umbral-mobile` audience in addition to `umbral-web`.
- Per-participant team-name **history** is OUT of SP-1 scope (deferred, additive, non-blocking).
- TDD per task; run the focused tests for the slice; full service test suite green before each commit; frequent commits.
- Databases are dropped/recreated (academic project) — no data migration.
- Do not reintroduce obsolete doctrine; old names may appear only as negations/historical/legacy/marked-debt. SP-1's R1 gate (Task 13) enforces this.

## File Structure

**Moved into Identity (source `services/team-service/...` → dest `services/identity-service/...`), namespace `Umbral.TeamService.*` → `Umbral.IdentityService.*`:**
- `Domain/Entities/{Equipo,ParticipanteEquipo}.cs` (Equipo loses `CodigoAcceso`).
- `Domain/Enums/{EstadoEquipo,ResultadoSalidaEquipo}.cs`.
- `Domain/Exceptions/` — the 6 team domain exceptions.
- `Domain/Abstractions/Persistence/IEquipoRepository.cs` (Identity keeps repo interfaces in Domain; strip the 3 access-code methods).
- `Application/Teams/{CreateTeam,LeaveTeam,TransferLeadership}/` (drop code-gen/retry).
- `Application/Teams/Exceptions/` — kept app exceptions (dedupe `PersistenceException` against Identity's existing one).
- `Infrastructure/Persistence/EquipoRepository.cs` (strip access-code locking/queries).
- `Infrastructure/Events/{ITeamEventsPublisher→IEquipoEventsPublisher, NoOp→Rabbit or NoOp}.cs`.

**Created (new `InvitacionEquipo`):**
- `Domain/Entities/InvitacionEquipo.cs`, `Domain/Enums/EstadoInvitacion.cs`, `Domain/Exceptions/{InvitacionNoEncontrada,UsuarioYaEnEquipo,EquipoLleno,NoEsLider,InvitacionNoPendiente}Exception.cs`, `Domain/Abstractions/Persistence/IInvitacionEquipoRepository.cs`.
- `Application/Teams/Invitations/{EnviarInvitacion,AceptarInvitacion,RechazarInvitacion}/` + `Queries/{GetInvitacionesRecibidas,GetParticipantesElegibles}/`.
- `Infrastructure/Persistence/InvitacionEquipoRepository.cs` + EF config in `IdentityDbContext`.

**Deleted:** `ICodigoAccesoGenerator`, `CodigoAccesoGenerator`, `Teams/JoinTeamByCode/`, `AccessCodeGenerationException`, `TeamNotFoundByAccessCodeException`, all HU-04 tests, the `team-service` tree, the compose `team-service` block + `umbral_team`.

**Mobile:** delete join-by-code stack; add invitations inbox + invite-member; repoint env; update navigation.

---

### Task 1: Identity host prep — `ParticipantOnly` policy, `umbral-mobile` audience, table bootstrap

**Files:**
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Program.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/appsettings.json` (valid audiences)
- Test: `services/identity-service/tests/Umbral.IdentityService.ContractTests/` (new `ParticipantPolicyContractTests.cs`)

**Interfaces:**
- Produces: an authorization policy named `ParticipantOnly` (role `Participante`) and `umbral-mobile` accepted as a valid JWT audience, both consumed by the team/invitation endpoints in Task 8.

- [ ] **Step 1: Write the failing contract test**

Create `services/identity-service/tests/Umbral.IdentityService.ContractTests/ParticipantPolicyContractTests.cs`. Add a temporary `GET /api/identity/_participant-ping` endpoint guarded by `ParticipantOnly` (added in Step 3, removed at end of SP-1) and assert: role `Participante` → 200; role `Administrador` → 403; no auth → 401. Use the existing `TestAuthHandler` pattern (headers `X-Test-Role`, `X-Test-UserId`) — copy it from `team-service`'s contract tests into Identity's contract test project if not present.

```csharp
[Fact]
public async Task ParticipantPing_Returns200_ForParticipante()
{
    var client = _factory.CreateClientAs(role: "Participante", userId: Guid.NewGuid());
    var res = await client.GetAsync("/api/identity/_participant-ping");
    Assert.Equal(HttpStatusCode.OK, res.StatusCode);
}

[Fact]
public async Task ParticipantPing_Returns403_ForAdministrador()
{
    var client = _factory.CreateClientAs(role: "Administrador", userId: Guid.NewGuid());
    var res = await client.GetAsync("/api/identity/_participant-ping");
    Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test services/identity-service/tests/Umbral.IdentityService.ContractTests/ --filter ParticipantPing`
Expected: FAIL (policy/endpoint not defined, or 404).

- [ ] **Step 3: Add the policy, audience, and temporary ping**

In `Program.cs`, in `AddAuthorization`, add:

```csharp
options.AddPolicy("ParticipantOnly", policy => policy.RequireRole("Participante"));
```

In the JWT `TokenValidationParameters`, ensure `ValidAudiences` includes `umbral-mobile` (read from config `Keycloak:ValidAudiences`); add `umbral-mobile` to `appsettings.json` audiences list. Add the temporary endpoint:

```csharp
app.MapGet("/api/identity/_participant-ping", () => Results.Ok(new { ok = true }))
   .RequireAuthorization("ParticipantOnly");
```

- [ ] **Step 4: Confirm `IdentityDbContext` startup will create new tables**

Read `Program.cs`'s DB-init block (currently `EnsureCreatedAsync` + raw SQL for `usuarios`). Confirm that adding new `DbSet`s in later tasks will be created by the same path; if Identity relies on raw-SQL DDL rather than `EnsureCreated` for table creation, note that Tasks 4 and 7 must extend the startup DDL (or the implementer adds the new tables' `CREATE TABLE` there). Do not switch to EF migrations in SP-1 — match the existing bootstrap pattern. (No code change in this step; this is a recorded decision the later tasks depend on.)

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test services/identity-service/tests/Umbral.IdentityService.ContractTests/ --filter ParticipantPing`
Expected: PASS (200 / 403 / 401).

- [ ] **Step 6: Run the full Identity suite and commit**

Run: `dotnet test services/identity-service/<Identity solution or all test projects>`
Expected: all green.

```bash
git add services/identity-service
git commit -m "Identity: add ParticipantOnly policy and umbral-mobile audience for incoming Equipos context"
```

---

### Task 2: Move Equipos Domain into Identity (drop `CodigoAcceso`)

**Files:**
- Create (moved): `services/identity-service/src/Umbral.IdentityService.Domain/Entities/{Equipo,ParticipanteEquipo}.cs`
- Create (moved): `services/identity-service/src/Umbral.IdentityService.Domain/Enums/{EstadoEquipo,ResultadoSalidaEquipo}.cs`
- Create (moved): `services/identity-service/src/Umbral.IdentityService.Domain/Exceptions/` — the 6 team domain exceptions
- Create (moved): `services/identity-service/src/Umbral.IdentityService.Domain/Abstractions/Persistence/IEquipoRepository.cs`
- Test: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Teams/` (re-homed domain tests)

**Interfaces:**
- Produces: `Equipo.CrearPorParticipante(string nombreEquipo, Guid creadorUserId)`, `Equipo.AgregarParticipante(Guid usuarioId)`, `Equipo.Salir(Guid)`, `Equipo.TransferirLiderazgo(Guid actor, Guid nuevo)`; `IEquipoRepository` with `ExistsActiveTeamByUserIdAsync`, `GetActiveByMemberUserIdAsync`, `AddAsync`, `UpdateAsync` (no access-code methods). Consumed by Tasks 3, 4, 5, 6.

- [ ] **Step 1: Move the domain files with namespace rewrite**

`git mv` each domain file from `services/team-service/src/Umbral.TeamService.Domain/...` to the Identity Domain paths above. In every moved file, change `namespace Umbral.TeamService.Domain.*` → `namespace Umbral.IdentityService.Domain.*` and fix `using` lines accordingly. Move `IEquipoRepository.cs` from team-service's `Application/Abstractions/Persistence/` into Identity's `Domain/Abstractions/Persistence/` (Domain owns infra interfaces).

- [ ] **Step 2: Strip `CodigoAcceso` from `Equipo` and `IEquipoRepository`**

In `Equipo.cs`: delete the `CodigoAcceso` property (line ~12), its init in the private ctor (~19), its validation (~29-31), and its assignment (~36). Change the factory to:

```csharp
public static Equipo CrearPorParticipante(string nombreEquipo, Guid creadorUserId)
{
    return new Equipo(nombreEquipo, creadorUserId);
}

private Equipo(string nombreEquipo, Guid creadorUserId)
{
    if (string.IsNullOrWhiteSpace(nombreEquipo))
        throw new ArgumentException("NombreEquipo requerido", nameof(nombreEquipo));
    EquipoId = Guid.NewGuid();
    NombreEquipo = nombreEquipo.Trim();
    Estado = EstadoEquipo.Activo;
    Participantes.Add(ParticipanteEquipo.CrearCreador(creadorUserId));
    EnsureCardinalityInvariant();
}
```

In `IEquipoRepository.cs`: delete `ExistsByAccessCodeAsync`, `GetActiveByAccessCodeAsync`, `ExecuteWithAccessCodeLockAsync<T>`. Keep `ExistsActiveTeamByUserIdAsync`, `GetActiveByMemberUserIdAsync`, `AddAsync`, `UpdateAsync`.

- [ ] **Step 2b: Re-home the domain tests, removing the code argument**

`git mv` the team domain tests (`CrearEquipoDomainTests.cs`, `UnirseAEquipoDomainTests.cs`, `TransferirLiderazgoDomainTests.cs`, `SalirDeEquipoDomainTests.cs`) into `services/identity-service/tests/Umbral.IdentityService.UnitTests/Teams/`, rewrite namespaces, and change every `Equipo.CrearPorParticipante("Name", "ABCD2345", id)` to `Equipo.CrearPorParticipante("Name", id)`. (Keep `UnirseAEquipoDomainTests` — it tests `AgregarParticipante`, still valid; it is the *handler* HU-04 tests that are deleted, not the domain add-member test.)

- [ ] **Step 3: Build and run the re-homed domain tests**

Run: `dotnet test services/identity-service/tests/Umbral.IdentityService.UnitTests/ --filter Teams`
Expected: PASS (cardinality, leader, leave, transfer, add-member). If the Identity Domain project does not yet reference these, ensure the test project references `Umbral.IdentityService.Domain`.

- [ ] **Step 4: Commit**

```bash
git add services/identity-service
git commit -m "Move Equipos domain into Identity; remove CodigoAcceso from Equipo and IEquipoRepository"
```

---

### Task 3: Move CreateTeam / LeaveTeam / TransferLeadership application slices (no code-gen)

**Files:**
- Create (moved): `services/identity-service/src/Umbral.IdentityService.Application/Teams/{CreateTeam,LeaveTeam,TransferLeadership}/*`
- Create (moved): `services/identity-service/src/Umbral.IdentityService.Application/Teams/Exceptions/*` (dedupe `PersistenceException`)
- Modify: `services/identity-service/src/Umbral.IdentityService.Application/DependencyInjection.cs`
- Test: re-homed handler+validator tests under `Umbral.IdentityService.UnitTests/Teams/`

**Interfaces:**
- Consumes: `Equipo`, `IEquipoRepository` (Task 2).
- Produces: `CrearEquipoCommand(Guid ActorUserId, string NombreEquipo)` → `CrearEquipoResponse` (no `CodigoAcceso`); `SalirDeEquipoCommand`; `TransferirLiderazgoCommand`. Consumed by Task 8.

- [ ] **Step 1: Move the three slices with namespace rewrite**

`git mv` `Teams/CreateTeam/`, `Teams/LeaveTeam/`, `Teams/TransferLeadership/` into Identity's `Application/Teams/`, rewriting `Umbral.TeamService.Application.*` → `Umbral.IdentityService.Application.*`. Do **not** move `Teams/JoinTeamByCode/` (deleted in Task 9).

- [ ] **Step 2: Remove code-generation from `CrearEquipoCommandHandler`**

Delete the `ICodigoAccesoGenerator` dependency, the `MaxPersistenceCodeCollisionRetries` retry loop, the `GenerateUniqueCodeAsync` call, and the `AccessCodeGenerationException` catch. The handler becomes: check `ExistsActiveTeamByUserIdAsync` → throw `AlreadyBelongsToActiveTeamException`; `Equipo.CrearPorParticipante(nombreEquipo, actorUserId)`; `AddAsync`; publish `EquipoCreadoIntegrationEvent` (without `CodigoAcceso` — see Task 4). Remove `CodigoAcceso` from `CrearEquipoResponse` and its mapping.

- [ ] **Step 3: Move and dedupe app exceptions**

`git mv` the kept app exceptions into `Application/Teams/Exceptions/`: `AlreadyBelongsToActiveTeamException`, `ConcurrentTeamCreationException`, `NoActiveTeamForParticipantException`, `LeaveTeamConflictException`, `TransferirLiderazgoConflictException`, `TeamFullException`, `ParticipantAlreadyInTargetTeamException`, `UniqueMembershipConflictException`. **Do NOT** move team-service's `PersistenceException` — Identity already has `Umbral.IdentityService.Application/Exceptions/PersistenceException.cs`; update the moved handlers/repos to use Identity's. Delete team-service's `AccessCodeGenerationException` and `TeamNotFoundByAccessCodeException` (not moved).

- [ ] **Step 4: Wire DI**

`Application/DependencyInjection.cs` uses MediatR + FluentValidation auto-discovery from the assembly, so the moved slices register automatically. Confirm no manual `ICodigoAccesoGenerator` registration remains anywhere in Application.

- [ ] **Step 5: Re-home handler/validator tests (de-coded)**

`git mv` `CrearEquipoHandlerTests.cs`, `CrearEquipoValidatorTests.cs`, `SalirDeEquipo{Handler}Tests.cs`, `TransferirLiderazgo{Handler}Tests.cs` into the Identity unit test project. In `CrearEquipoHandlerTests`: delete `FakeCodigoAccesoGenerator`, `ThrowingCodigoAccesoGenerator`, `Should_Throw_When_AccessCodeGeneration_Fails`, `Should_Retry_When_AccessCode_Collision...`, and all `response.CodigoAcceso` asserts. Keep `Should_Create_Team_When_User_Is_Not_In_ActiveTeam` and `Should_Throw_When_User_Already_Belongs_To_ActiveTeam`.

- [ ] **Step 6: Run the team application tests and commit**

Run: `dotnet test services/identity-service/tests/Umbral.IdentityService.UnitTests/ --filter Teams`
Expected: PASS.

```bash
git add services/identity-service
git commit -m "Move team CreateTeam/LeaveTeam/TransferLeadership slices into Identity; drop access-code generation"
```

---

### Task 4: Equipos persistence in `IdentityDbContext` + `EquipoRepository` (no access code)

**Files:**
- Modify: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Persistence/IdentityDbContext.cs`
- Create (moved): `services/identity-service/src/Umbral.IdentityService.Infrastructure/Persistence/EquipoRepository.cs`
- Create (moved): `services/identity-service/src/Umbral.IdentityService.Infrastructure/Events/{IEquipoEventsPublisher,NoOpEquipoEventsPublisher}.cs` + `EquipoCreadoIntegrationEvent`
- Modify: `services/identity-service/src/Umbral.IdentityService.Infrastructure/DependencyInjection.cs`
- Modify: `Program.cs` startup DDL if Identity bootstraps tables via raw SQL (see Task 1 Step 4)
- Test: `Umbral.IdentityService.IntegrationTests/Teams/` (in-memory create/leave/transfer)

**Interfaces:**
- Consumes: `Equipo`, `IEquipoRepository`, the moved application slices.
- Produces: `IdentityDbContext.Equipos`, `IdentityDbContext.ParticipantesEquipo`; `EquipoRepository : IEquipoRepository`; `IEquipoEventsPublisher` with `EquipoCreadoIntegrationEvent(Guid EquipoId, Guid LiderUserId, DateTime OccurredOnUtc)` (no `CodigoAcceso`).

- [ ] **Step 1: Add Equipos DbSets + EF config (no `codigoacceso`)**

In `IdentityDbContext.cs`, add `DbSet<Equipo> Equipos` (table `equipos`, PK `equipoid`; **no `codigoacceso` column, no `ux_equipos_codigoacceso` index**) and `DbSet<ParticipanteEquipo> ParticipantesEquipo` (table `equipos_participantes`, PK `participanteequipoid`, keep unique index `ux_equipos_participantes_usuarioid`, FK `equipoid` cascade). Mirror the existing `TeamDbContext` config minus the access-code column/index.

- [ ] **Step 2: Move `EquipoRepository`, stripping access-code branches**

`git mv` `EquipoRepository.cs` into Identity's `Infrastructure/Persistence/`, rewrite namespace, and delete: `ExecuteWithAccessCodeLockAsync` (advisory-lock impl), `GetActiveByAccessCodeAsync`, `ExistsByAccessCodeAsync`, and the `ux_equipos_codigoacceso` constraint-violation catch. Keep the `ux_equipos_participantes_usuarioid` catch (maps to `UniqueMembershipConflictException`) and the kept query/add/update methods, using Identity's `PersistenceException`.

- [ ] **Step 3: Re-home the events publisher (drop `CodigoAcceso`)**

`git mv` `ITeamEventsPublisher`/`NoOpTeamEventsPublisher` into Identity, rename to `IEquipoEventsPublisher`/`NoOpEquipoEventsPublisher`, and change `EquipoCreadoIntegrationEvent` to `(Guid EquipoId, Guid LiderUserId, DateTime OccurredOnUtc)` — remove the `CodigoAcceso` field. Update `CrearEquipoCommandHandler` to construct it without the code.

- [ ] **Step 4: Register in Infrastructure DI + ensure tables created**

In `Infrastructure/DependencyInjection.cs` register `IEquipoRepository→EquipoRepository` and `IEquipoEventsPublisher→NoOpEquipoEventsPublisher`. If Identity bootstraps tables via raw SQL in `Program.cs`, add `CREATE TABLE equipos (...)` and `CREATE TABLE equipos_participantes (...)` (with the unique index, no `codigoacceso`); otherwise confirm `EnsureCreatedAsync` covers the new DbSets.

- [ ] **Step 5: Write + run an in-memory integration test for create/leave/transfer**

Add `Umbral.IdentityService.IntegrationTests/Teams/EquipoPersistenceTests.cs`: create a team, leave as non-leader, transfer leadership, assert persisted state (exactly 1 leader; member removed). Use the in-memory provider factory pattern.

Run: `dotnet test services/identity-service/tests/Umbral.IdentityService.IntegrationTests/ --filter Teams`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add services/identity-service
git commit -m "Persist Equipos in IdentityDbContext; EquipoRepository and events without access code"
```

---

### Task 5: `InvitacionEquipo` domain

**Files:**
- Create: `services/identity-service/src/Umbral.IdentityService.Domain/Entities/InvitacionEquipo.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Domain/Enums/EstadoInvitacion.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Domain/Exceptions/{NoEsLiderException,UsuarioYaEnEquipoException,EquipoLlenoException,InvitacionNoPendienteException}.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Domain/Abstractions/Persistence/IInvitacionEquipoRepository.cs`
- Test: `Umbral.IdentityService.UnitTests/Teams/InvitacionEquipoDomainTests.cs`

**Interfaces:**
- Produces: `InvitacionEquipo.Crear(Guid equipoId, Guid invitadoUserId, Guid invitadoPorUserId)`, `.Aceptar()`, `.Rechazar()`, `EstadoInvitacion` enum; `IInvitacionEquipoRepository`. Consumed by Tasks 6, 7, 8.

- [ ] **Step 1: Write the failing domain tests**

```csharp
// InvitacionEquipoDomainTests.cs
[Fact] public void Crear_Should_StartPendiente() {
    var inv = InvitacionEquipo.Crear(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    Assert.Equal(EstadoInvitacion.Pendiente, inv.Estado);
}
[Fact] public void Aceptar_Should_SetAceptada() {
    var inv = InvitacionEquipo.Crear(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    inv.Aceptar();
    Assert.Equal(EstadoInvitacion.Aceptada, inv.Estado);
}
[Fact] public void Aceptar_Should_Throw_When_NotPendiente() {
    var inv = InvitacionEquipo.Crear(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    inv.Rechazar();
    Assert.Throws<InvitacionNoPendienteException>(() => inv.Aceptar());
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test services/identity-service/tests/Umbral.IdentityService.UnitTests/ --filter InvitacionEquipoDomain`
Expected: FAIL (types not defined).

- [ ] **Step 3: Implement the domain types**

```csharp
// EstadoInvitacion.cs
namespace Umbral.IdentityService.Domain.Enums;
public enum EstadoInvitacion { Pendiente = 1, Aceptada = 2, Rechazada = 3 }
```

```csharp
// InvitacionEquipo.cs
namespace Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Domain.Exceptions;

public sealed class InvitacionEquipo
{
    public Guid InvitacionEquipoId { get; private set; }
    public Guid EquipoId { get; private set; }
    public Guid InvitadoUserId { get; private set; }
    public Guid InvitadoPorUserId { get; private set; }
    public EstadoInvitacion Estado { get; private set; }
    public DateTime FechaCreacionUtc { get; private set; }

    private InvitacionEquipo() { }

    public static InvitacionEquipo Crear(Guid equipoId, Guid invitadoUserId, Guid invitadoPorUserId)
    {
        if (equipoId == Guid.Empty) throw new ArgumentException("EquipoId requerido", nameof(equipoId));
        if (invitadoUserId == Guid.Empty) throw new ArgumentException("InvitadoUserId requerido", nameof(invitadoUserId));
        if (invitadoPorUserId == Guid.Empty) throw new ArgumentException("InvitadoPorUserId requerido", nameof(invitadoPorUserId));
        return new InvitacionEquipo
        {
            InvitacionEquipoId = Guid.NewGuid(),
            EquipoId = equipoId,
            InvitadoUserId = invitadoUserId,
            InvitadoPorUserId = invitadoPorUserId,
            Estado = EstadoInvitacion.Pendiente,
            FechaCreacionUtc = DateTime.UtcNow,
        };
    }

    public void Aceptar()
    {
        if (Estado != EstadoInvitacion.Pendiente) throw new InvitacionNoPendienteException(InvitacionEquipoId);
        Estado = EstadoInvitacion.Aceptada;
    }

    public void Rechazar()
    {
        if (Estado != EstadoInvitacion.Pendiente) throw new InvitacionNoPendienteException(InvitacionEquipoId);
        Estado = EstadoInvitacion.Rechazada;
    }
}
```

Create the exceptions (each `sealed class ... : Exception` with a Spanish message), e.g.:

```csharp
namespace Umbral.IdentityService.Domain.Exceptions;
public sealed class InvitacionNoPendienteException : InvalidOperationException
{
    public InvitacionNoPendienteException(Guid invitacionId)
        : base($"La invitacion '{invitacionId}' no esta pendiente.") { }
}
```

`NoEsLiderException(Guid actorUserId)`, `UsuarioYaEnEquipoException(Guid userId)`, `EquipoLlenoException(Guid equipoId)` follow the same shape.

```csharp
// IInvitacionEquipoRepository.cs
namespace Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;
public interface IInvitacionEquipoRepository
{
    Task AddAsync(InvitacionEquipo invitacion, CancellationToken ct);
    Task UpdateAsync(InvitacionEquipo invitacion, CancellationToken ct);
    Task<InvitacionEquipo?> GetByIdAsync(Guid invitacionId, CancellationToken ct);
    Task<IReadOnlyList<InvitacionEquipo>> GetPendientesByInvitadoAsync(Guid invitadoUserId, CancellationToken ct);
    Task<bool> ExistsPendienteAsync(Guid equipoId, Guid invitadoUserId, CancellationToken ct);
    Task DeletePendientesByEquipoAsync(Guid equipoId, CancellationToken ct);
}
```

- [ ] **Step 4: Run to verify pass; commit**

Run: `dotnet test services/identity-service/tests/Umbral.IdentityService.UnitTests/ --filter InvitacionEquipoDomain` → PASS.

```bash
git add services/identity-service
git commit -m "Add InvitacionEquipo domain (entity, enum, exceptions, repository interface)"
```

---

### Task 6: `InvitacionEquipo` application (send/accept/reject + inbox + eligible list)

**Files:**
- Create: `Application/Teams/Invitations/EnviarInvitacion/{EnviarInvitacionEquipoCommand,Handler,Validator,Response}.cs`
- Create: `Application/Teams/Invitations/AceptarInvitacion/{AceptarInvitacionEquipoCommand,Handler,Validator,Response}.cs`
- Create: `Application/Teams/Invitations/RechazarInvitacion/{RechazarInvitacionEquipoCommand,Handler,Validator,Response}.cs`
- Create: `Application/Teams/Queries/GetInvitacionesRecibidas/{Query,Handler,Response}.cs`
- Create: `Application/Teams/Queries/GetParticipantesElegibles/{Query,Handler,Response}.cs`
- Test: `Umbral.IdentityService.UnitTests/Teams/Invitations/*`

**Interfaces:**
- Consumes: `Equipo`, `IEquipoRepository`, `InvitacionEquipo`, `IInvitacionEquipoRepository`, `IUsuarioRepository`.
- Produces: `EnviarInvitacionEquipoCommand(Guid ActorUserId, Guid InvitadoUserId)`, `AceptarInvitacionEquipoCommand(Guid ActorUserId, Guid InvitacionId)`, `RechazarInvitacionEquipoCommand(Guid ActorUserId, Guid InvitacionId)`, `GetInvitacionesRecibidasQuery(Guid ActorUserId)`, `GetParticipantesElegiblesQuery(Guid ActorUserId)`. Consumed by Task 8.

- [ ] **Step 1: Write failing handler tests**

Add `EnviarInvitacionEquipoHandlerTests.cs`, `AceptarInvitacionEquipoHandlerTests.cs`, `GetParticipantesElegiblesHandlerTests.cs` covering: only the leader of the actor's active team can invite (`NoEsLiderException` otherwise); cannot invite a user already in a team (`UsuarioYaEnEquipoException`); cannot invite when team is full (`EquipoLlenoException`); accepting a pending invitation adds the invitee via `Equipo.AgregarParticipante` and marks `Aceptada`; the eligible list excludes users already in any active team and returns empty when the team is full.

```csharp
[Fact] public async Task Enviar_Throws_When_Actor_Not_Leader() { /* arrange actor in team but not leader */ await Assert.ThrowsAsync<NoEsLiderException>(() => _handler.Handle(cmd, default)); }
[Fact] public async Task Aceptar_Adds_Member_And_Marks_Aceptada() { /* arrange pending invite + team with room */ var res = await _handler.Handle(cmd, default); /* assert member added + invite Aceptada */ }
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test ...UnitTests/ --filter Invitations` → FAIL.

- [ ] **Step 3: Implement commands/handlers/validators/queries**

`EnviarInvitacionEquipoCommandHandler`: load the actor's active team via `GetActiveByMemberUserIdAsync`; if none or actor is not its leader → `NoEsLiderException`; if `Participantes.Count >= 5` → `EquipoLlenoException`; if the invitee already has an active team (`ExistsActiveTeamByUserIdAsync`) → `UsuarioYaEnEquipoException`; if a pending invite already exists (`ExistsPendienteAsync`) → no-op or conflict; else `InvitacionEquipo.Crear(equipoId, invitadoUserId, actorUserId)` → `AddAsync` → publish `InvitacionEquipoCreada`.

`AceptarInvitacionEquipoCommandHandler`: load the invite (`GetByIdAsync`); guard the actor is the invitee; re-check the invitee has no active team and the team has room; `invite.Aceptar()`; load the team and `equipo.AgregarParticipante(invitadoUserId)`; `UpdateAsync` both; publish `InvitacionEquipoAceptada`.

`RechazarInvitacionEquipoCommandHandler`: load invite; guard actor is invitee; `invite.Rechazar()`; `UpdateAsync`; publish `InvitacionEquipoRechazada`.

`GetInvitacionesRecibidasQueryHandler`: `GetPendientesByInvitadoAsync(actorUserId)` → list of `(InvitacionId, EquipoId, NombreEquipo, InvitadoPorUserId, FechaCreacionUtc)`.

`GetParticipantesElegiblesQueryHandler`: load the actor's active team (must be leader); if full → empty list; else `IUsuarioRepository.GetAllAsync` filtered to role `Participante` and excluding any user with an active team (`ExistsActiveTeamByUserIdAsync` or a batch query) and excluding current members → `(UserId, Nombre, Correo)`.

Validators: `ActorUserId`/`InvitadoUserId`/`InvitacionId` `NotEmpty`.

- [ ] **Step 4: Run to verify pass; commit**

Run: `dotnet test ...UnitTests/ --filter Invitations` → PASS.

```bash
git add services/identity-service
git commit -m "Add InvitacionEquipo application: send/accept/reject commands, inbox and eligible-participants queries"
```

---

### Task 7: `InvitacionEquipo` persistence

**Files:**
- Modify: `IdentityDbContext.cs` (add `DbSet<InvitacionEquipo> InvitacionesEquipo` + config)
- Create: `Infrastructure/Persistence/InvitacionEquipoRepository.cs`
- Modify: `Infrastructure/DependencyInjection.cs`; `Program.cs` DDL if applicable
- Test: `Umbral.IdentityService.IntegrationTests/Teams/InvitacionEquipoPersistenceTests.cs`

**Interfaces:**
- Produces: `InvitacionEquipoRepository : IInvitacionEquipoRepository`; table `invitaciones_equipo`.

- [ ] **Step 1: Add DbSet + EF config**

In `IdentityDbContext.cs`, add `DbSet<InvitacionEquipo> InvitacionesEquipo` → table `invitaciones_equipo` (PK `invitacionequipoid`; columns `equipoid`, `invitadouserid`, `invitadoporuserid`, `estado` (int), `fechacreacionutc`; index on `invitadouserid`; FK `equipoid` → `equipos` cascade).

- [ ] **Step 2: Implement the repository**

`InvitacionEquipoRepository : IInvitacionEquipoRepository` over `IdentityDbContext`, implementing the 6 interface methods; `DbUpdateException` → Identity's `PersistenceException`. `DeletePendientesByEquipoAsync` removes pending rows for a team (used when a team is deleted on sole-leader leave — wire this into `SalirDeEquipoCommandHandler` when `ResultadoSalidaEquipo.EquipoEliminado`).

- [ ] **Step 3: Register DI + table bootstrap**

Register `IInvitacionEquipoRepository→InvitacionEquipoRepository`; add `CREATE TABLE invitaciones_equipo (...)` to startup DDL if Identity bootstraps via raw SQL.

- [ ] **Step 4: Integration test**

`InvitacionEquipoPersistenceTests.cs`: create team → invite a participant → accept → assert invitee is a member and invite is `Aceptada`; invite + reject → assert `Rechazada`; sole-leader leaves → assert pending invites for the team are deleted.

Run: `dotnet test ...IntegrationTests/ --filter Invitacion` → PASS.

- [ ] **Step 5: Commit**

```bash
git add services/identity-service
git commit -m "Persist InvitacionEquipo in IdentityDbContext; delete-pending-on-team-deletion"
```

---

### Task 8: Identity API — team + invitation endpoints (`ParticipantOnly`)

**Files:**
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Program.cs`
- Create (moved): Identity Api auth helper `AuthenticatedUserClaims` if not already present (reuse Identity's `KeycloakRoleClaims`)
- Test: `Umbral.IdentityService.ContractTests/Teams/*`

**Interfaces:**
- Consumes: all Task 3 + Task 6 commands/queries.
- Produces: the HTTP surface (below) under `ParticipantOnly`.

- [ ] **Step 1: Write failing contract tests**

Re-home `Hu03ContractTests` (create, **no `codigoAcceso` in response**), `Hu06ContractTests` (transfer), `Hu07ContractTests` (leave) into `Umbral.IdentityService.ContractTests/Teams/`, rewriting `WebApplicationFactory<Program>` to the Identity `Program` and replacing the `CreateTeamAndGetAccessCodeAsync` helper with an invitation-based join helper (create team → invite → accept). Add new contract tests `InvitationsContractTests`: leader invites (`POST /api/teams/invitations` → 201), invitee inbox (`GET /api/teams/invitations` → 200 list), accept (`POST /api/teams/invitations/{id}/acceptance` → 200), reject (`POST .../rejection` → 200), eligible list (`GET /api/teams/eligible-participants` → 200 excludes already-in-team), full-team invite → 409, non-leader invite → 403/409.

- [ ] **Step 2: Run to verify failure** — `dotnet test ...ContractTests/ --filter Teams` → FAIL (endpoints absent).

- [ ] **Step 3: Add the endpoints to Identity `Program.cs`**

Map, all `.RequireAuthorization("ParticipantOnly")`, `ActorUserId` from JWT `sub` via the auth helper:
- `POST /api/teams` → `CrearEquipoCommand` (201; response without `codigoAcceso`).
- `DELETE /api/teams/membership` → `SalirDeEquipoCommand` (200).
- `PATCH /api/teams/leadership` → `TransferirLiderazgoCommand` (200; req `{ nuevoLiderUserId }`).
- `POST /api/teams/invitations` → `EnviarInvitacionEquipoCommand` (201; req `{ invitadoUserId }`).
- `GET /api/teams/invitations` → `GetInvitacionesRecibidasQuery` (200).
- `POST /api/teams/invitations/{invitacionId:guid}/acceptance` → `AceptarInvitacionEquipoCommand` (200).
- `POST /api/teams/invitations/{invitacionId:guid}/rejection` → `RechazarInvitacionEquipoCommand` (200).
- `GET /api/teams/eligible-participants` → `GetParticipantesElegiblesQuery` (200).

Map exceptions to status: `AlreadyBelongsToActiveTeamException`/`ConcurrentTeamCreationException`/`ParticipantAlreadyInTargetTeamException`/`TeamFullException`/`EquipoLlenoException`/`UsuarioYaEnEquipoException`/`LeaveTeamConflictException`/`TransferirLiderazgoConflictException` → 409; `NoActiveTeamForParticipantException`/`InvitacionNoEncontradaException` → 404; `NoEsLiderException` → 403; `PersistenceException` → 500. (No access-code exceptions remain.)

- [ ] **Step 4: Run to verify pass** — `dotnet test ...ContractTests/ --filter Teams` → PASS.

- [ ] **Step 5: Full Identity suite + commit**

Run: `dotnet test` over all Identity test projects → green.

```bash
git add services/identity-service
git commit -m "Expose team + invitation endpoints in Identity API under ParticipantOnly"
```

---

### Task 9: Delete the access-code stack and the team-service tree

**Files:**
- Delete: `team-service` files already superseded; then the whole `services/team-service/` tree and its `.sln`/csproj once green.

- [ ] **Step 1: Delete the obsolete source**

`git rm` the team-service `ICodigoAccesoGenerator`, `CodigoAccesoGenerator`, `Teams/JoinTeamByCode/`, `AccessCodeGenerationException`, `TeamNotFoundByAccessCodeException`, and the HU-04 tests (`UnirseAEquipoHandlerTests`, `UnirseAEquipoValidatorTests`, `Hu04ContractTests`, `Hu04EndpointsIntegrationTests`, `Hu04PostgresConcurrencyTests`).

- [ ] **Step 2: Delete the team-service tree**

After confirming everything moved is referenced from Identity and Identity builds, `git rm -r services/team-service`. Remove the team-service project from any aggregate `.sln`.

- [ ] **Step 3: Verify no dangling references**

Run: `rg -n "Umbral\.TeamService" --glob '!**/_legacy/**'` → no hits in active code. `dotnet build` over the Identity solution → success.

- [ ] **Step 4: Commit**

```bash
git rm -r services/team-service
git commit -m "Delete team-service tree and the access-code stack (fully absorbed by Identity)"
```

---

### Task 10: Contracts — Identity team + invitation HTTP/event indexes

**Files:**
- Modify: `contracts/http/identity-api.md`, `contracts/events/identity-events.md`

- [ ] **Step 1: Update the HTTP contract**

In `contracts/http/identity-api.md`, register the team + invitation endpoints from Task 8 in the endpoint table (gateway path family `/api/identity/*`; note teams are `ParticipantOnly`). State explicitly there is no `codigoAcceso`.

- [ ] **Step 2: Update the event contract**

In `contracts/events/identity-events.md`, register `EquipoCreado` (payload `{ equipoId, liderUserId, occurredOnUtc }` — no `codigoAcceso`), `InvitacionEquipoCreada`, `InvitacionEquipoAceptada`, `InvitacionEquipoRechazada`. Keep them as high-level index entries (no invented payload fields beyond what the handlers emit).

- [ ] **Step 3: Verify + commit**

Run: `rg -n "codigoAcceso|join-by-code|CodigoAcceso" contracts/http/identity-api.md contracts/events/identity-events.md` → no active hits.

```bash
git add contracts
git commit -m "Contracts: register Identity team + invitation endpoints/events (no access code)"
```

---

### Task 11: Mobile — replace join-by-code with invitations inbox + invite-member

**Files:**
- Delete: `mobile/src/features/teams/{JoinTeamScreen.tsx,JoinTeamScreenContainer.tsx,joinTeamApi.js,joinTeamFlow.js,joinTeamScreenModel.js}`
- Create: `mobile/src/features/teams/{InvitationsScreen.tsx,InvitationsScreenContainer.tsx,invitationsApi.js,InviteMemberScreen.tsx,InviteMemberScreenContainer.tsx,inviteMemberApi.js}`
- Modify: `mobile/src/features/teams/{createTeamApi.js,createTeamFlow.js}` (drop `codigoAcceso`), `mobile/src/navigation/RootNavigator.tsx`, `mobile/src/config/env.ts`
- Test: `mobile/tests/{invitationsFlow.test.js,inviteMemberFlow.test.js}`; update `createTeamFlow.test.js`

**Interfaces:**
- Consumes: the Task 8 endpoints.

- [ ] **Step 1: Write failing mobile flow tests**

`mobile/tests/invitationsFlow.test.js` (node --test): `loadInvitations(apiBaseUrl, token)` → `GET /api/teams/invitations`; `acceptInvitation(id)` → `POST .../{id}/acceptance`; `rejectInvitation(id)` → `POST .../{id}/rejection`. `inviteMemberFlow.test.js`: `loadEligibleParticipants()` → `GET /api/teams/eligible-participants`; `sendInvitation(invitadoUserId)` → `POST /api/teams/invitations { invitadoUserId }`.

- [ ] **Step 2: Run to verify failure** — `cd mobile && npm test` → FAIL (modules absent).

- [ ] **Step 3: Implement the invitation api + flows + screens**

Add `invitationsApi.js`/`inviteMemberApi.js` (fetch + Bearer), the flow logic, and `InvitationsScreen`/`InviteMemberScreen` + containers following the existing Screen→Container→flow→api layering. Delete the join-by-code stack. In `createTeamApi.js`/`createTeamFlow.js` remove all `codigoAcceso` consumption. In `RootNavigator.tsx` remove the `JoinTeam` route; add `Invitations` and `InviteMember` (keep `CreateTeam`, `LeaveTeam`, `TransferLeadership`). In `env.ts` re-point the base URL to Identity/gateway (rename `EXPO_PUBLIC_TEAM_API_BASE_URL` → `EXPO_PUBLIC_IDENTITY_API_BASE_URL`; update `.env.example`).

- [ ] **Step 4: Run to verify pass** — `cd mobile && npm test && npm run typecheck` → PASS.

- [ ] **Step 5: Update mobile README + commit**

Update `mobile/README.md` (remove `codigoAcceso`/join-by-code references; describe the invitation flow).

```bash
git add mobile
git commit -m "Mobile: replace join-by-code with invitations inbox + invite-member; repoint to Identity"
```

---

### Task 12: Infra — remove team-service + `umbral_team` from compose

**Files:**
- Modify: `infra/docker-compose.yml`

- [ ] **Step 1: Edit compose**

Delete the `team-service` service block (port 5099, `ConnectionStrings__TeamDatabase`, `umbral_team`). Remove any `depends_on: team-service`. Ensure `identity-service` exposes a host port and its `KEYCLOAK_VALID_AUDIENCES` includes `umbral-mobile`. Standardize the Postgres password (`trivia-game-service` currently uses `umbral`; align to `16102005`). Remove the `umbral_team` database from any DB-creation docs/step.

- [ ] **Step 2: Verify + commit**

Run: `docker compose -f infra/docker-compose.yml config >/dev/null && echo OK` (validates compose). `rg -n "umbral_team|team-service|TeamDatabase" infra/docker-compose.yml` → no hits.

```bash
git add infra
git commit -m "Infra: remove team-service and umbral_team; Identity hosts teams (umbral-mobile audience)"
```

---

### Task 13: SP-1 R1 obsolete-doctrine gate

**Files:**
- Create: `scripts/detect-obsolete-doctrine.sh` (reusable detection script)

- [ ] **Step 1: Write the reusable detection script**

Create `scripts/detect-obsolete-doctrine.sh`: an `rg`-based scan for the shared ruleset (old names/folders/namespaces `Umbral.TeamService|TriviaGame|BdtGameService`, DBs `umbral_team|umbral_trivia_game|umbral_bdt_game`, aggregates `PartidaTrivia|PartidaBDT|CompetidorTrivia|ExploradorBDT|FormularioTrivia`, rules `CodigoAcceso|codigo de acceso|access code|EtapasGanadas`), excluding `_legacy/`, `_legacy-implementation-evidence/`, `.git`, `.superpowers`, and printing hits for triage. It accepts a path scope argument (default: SP-1 surface = `services/identity-service mobile/src/features/teams contracts infra`).

- [ ] **Step 2: Run the script over the SP-1 surface**

Run: `bash scripts/detect-obsolete-doctrine.sh services/identity-service mobile/src/features/teams contracts/http/identity-api.md contracts/events/identity-events.md infra/docker-compose.yml`
Expected: every hit is a negation/historical/legacy/marked-debt reference; **no active** `CodigoAcceso`/join-by-code/`Umbral.TeamService`/`umbral_team`.

- [ ] **Step 3: Dispatch the R1 multi-agent review**

Using superpowers:subagent-driven-development's final-review step (or an equivalent multi-agent adversarial workflow), review the SP-1 changeset against the shared detection ruleset and the SP-1 acceptance criteria: Equipos fully inside Identity, `InvitacionEquipo` flow working, no access-code, mobile adapted, contracts/compose updated, full Identity + mobile test suites green. Route any Critical/Important findings to one fix subagent; record Minors in the ledger.

- [ ] **Step 4: Commit the detection script + record R1 result**

```bash
git add scripts/detect-obsolete-doctrine.sh
git commit -m "Add reusable obsolete-doctrine detection script; record SP-1 R1 gate result"
```

---

## Self-Review Notes

**Spec coverage (against the migration design's SP-1 section):**
- Lift-and-reshape, Equipos into Identity → Tasks 2–4, 8, 9.
- Remove `CodigoAcceso`, add `InvitacionEquipo` → Tasks 2 (strip), 5–7 (new), 8 (endpoints), 9 (delete stack).
- `ParticipantOnly` + `umbral-mobile` audience → Task 1, 8.
- Contracts/events → Task 10. Mobile adaptation → Task 11. Infra/DB → Task 4/7/12. Tests re-homed/rewritten → Tasks 2,3,4,6,7,8,11. R1 gate + detection script → Task 13.
- Deferred (correctly absent): per-participant team-name history; the full YARP gateway (minimal/direct routing for SP-1); `Convocatoria`.

**Placeholder scan:** new `InvitacionEquipo` domain ships complete code; moved code is specified by exact source→dest + the precise doctrine edits; no "TBD"/"add validation"/"similar to" placeholders. Test steps name concrete assertions and commands.

**Type consistency:** `Equipo.CrearPorParticipante(string, Guid)`, `IEquipoRepository` (4 kept methods), `EquipoCreadoIntegrationEvent(Guid, Guid, DateTime)`, `InvitacionEquipo.Crear/Aceptar/Rechazar`, `EstadoInvitacion{Pendiente,Aceptada,Rechazada}`, and the command/query names are used identically across Tasks 2–8. Identity's existing `PersistenceException` is reused (team-service's dropped) consistently.

**Open implementation note for the executor:** Identity's table bootstrap mechanism (raw-SQL DDL vs `EnsureCreatedAsync`) must be confirmed in Task 1 Step 4; Tasks 4 and 7 follow whatever Task 1 establishes (no EF-migrations switch in SP-1).
