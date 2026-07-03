# SP-3e-1a — Identity `GET /api/teams/mine` (read de membresía) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Identity expone un endpoint read `GET /api/teams/mine` que devuelve el equipo **activo** del participante autenticado, con sus integrantes y el flag de liderazgo. Operaciones lo consume (SP-3e-1b) para preinscribir equipos.

**Architecture:** Query MediatR `ObtenerMiEquipoQuery(actorUserId)` → handler que usa el repo existente `IEquipoRepository.GetActiveByMemberUserIdAsync` → mapea a `EquipoMineResponse`. Endpoint GET en `TeamsController` (mismo patrón que `TeamInvitationsController.Recibidas`): resuelve el `userId` del token, `200` con el equipo o `404` si el caller no tiene equipo activo.

**Tech Stack:** .NET 8, Clean Architecture + CQRS/MediatR, xUnit, `WebApplicationFactory` (contract tests vía `IdentityApiFactory` + `CreateClientAs`).

## Global Constraints

- **Servicio:** todo el trabajo vive en `services/identity-service/`. Backend-only.
- **Sin repo nuevo:** `IEquipoRepository.GetActiveByMemberUserIdAsync(Guid userId, CancellationToken)` YA existe y devuelve el `Equipo` activo del que el usuario es miembro (o `null`). No se agrega método de repo ni migración.
- **Sin liderazgo forzado en el endpoint:** `mine` devuelve el equipo de **cualquier** miembro (líder o no); el flag `esLider` por integrante lo consume Operaciones para su propia validación. No se filtra por liderazgo aquí.
- **Auth:** `[Authorize(Policy = "ParticipantOnly")]` (heredado del controller); `userId` vía `AuthenticatedUserClaims.TryGetUserId(User, out var actorUserId)`.
- **JSON camelCase** (config existente): `equipoId`, `nombreEquipo`, `estado`, `participantes: [ { usuarioId, esLider } ]`.
- **Fakes a mano, sin Moq** en unit tests. TDD estricto: test que falla → correr y ver rojo → implementar mínimo → correr y ver verde → commit.
- **Carve-out git (NO commitear):** `docs/04-sdd/traceability-matrix.md`, `docs/superpowers/specs/2026-06-27-sp3c-runtime-trivia-design.md`, `docs/04-sdd/auditorias/`. `git add` SOLO archivos nombrados exactos — nunca `git add -A`/`.`/`docs/`. Prohibido `git checkout`/`restore`/`clean`/`stash`/`reset` de rango amplio.
- **Trailer de commit:** cada commit termina con exactamente `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>` (sin línea de sesión).
- **Comando de test:** `dotnet test <ruta-de-un-solo-.csproj>`. NO pasar dos rutas en un comando (falla `MSB1008`).

**Rutas de proyecto (para los comandos):**
- Unit: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Umbral.IdentityService.UnitTests.csproj`
- Contract: `services/identity-service/tests/Umbral.IdentityService.ContractTests/Umbral.IdentityService.ContractTests.csproj`

**Firmas clave (contrato entre tareas):**
- Tarea 1 produce: `EquipoMineResponse(Guid EquipoId, string NombreEquipo, string Estado, IReadOnlyList<MiembroEquipoResponse> Participantes)` y `MiembroEquipoResponse(Guid UsuarioId, bool EsLider)` (en `Application/DTOs/EquipoMineResponse.cs`); `ObtenerMiEquipoQuery(Guid ActorUserId) : IRequest<EquipoMineResponse?>`; `ObtenerMiEquipoQueryHandler`.
- Tarea 2 produce: endpoint `GET /api/teams/mine` (`TeamsController.MiEquipo`).

---

### Task 1: Query + handler + DTO de "mi equipo"

**Files:**
- Create: `services/identity-service/src/Umbral.IdentityService.Application/DTOs/EquipoMineResponse.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Queries/ObtenerMiEquipoQuery.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Handlers/Queries/ObtenerMiEquipoQueryHandler.cs`
- Test: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Teams/ObtenerMiEquipoQueryHandlerTests.cs`

**Interfaces:**
- Consumes: `IEquipoRepository.GetActiveByMemberUserIdAsync(Guid, CancellationToken) → Task<Equipo?>`; `Equipo { Guid EquipoId, string NombreEquipo, EstadoEquipo Estado, List<ParticipanteEquipo> Participantes }`; `ParticipanteEquipo { Guid UsuarioId, bool EsLider }`.
- Produces: `EquipoMineResponse`, `MiembroEquipoResponse`, `ObtenerMiEquipoQuery`, `ObtenerMiEquipoQueryHandler`.

- [ ] **Step 1: Escribir el test del handler (rojo)**

Crear `services/identity-service/tests/Umbral.IdentityService.UnitTests/Teams/ObtenerMiEquipoQueryHandlerTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.IdentityService.Application.Handlers.Queries;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Teams;

public class ObtenerMiEquipoQueryHandlerTests
{
    // Fake a mano de IEquipoRepository: solo GetActiveByMemberUserIdAsync se usa aquí.
    private sealed class FakeEquipoRepository : IEquipoRepository
    {
        public Equipo? Activo;
        public Task<Equipo?> GetActiveByMemberUserIdAsync(Guid userId, CancellationToken ct) => Task.FromResult(Activo);
        public Task<bool> ExistsActiveTeamByUserIdAsync(Guid userId, CancellationToken ct) => Task.FromResult(Activo is not null);
        public Task<Equipo?> GetByIdAsync(Guid equipoId, CancellationToken ct) => Task.FromResult(Activo);
        public Task AddAsync(Equipo equipo, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateAsync(Equipo equipo, CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    public async Task Sin_equipo_activo_devuelve_null()
    {
        var handler = new ObtenerMiEquipoQueryHandler(new FakeEquipoRepository { Activo = null });

        var result = await handler.Handle(new ObtenerMiEquipoQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Con_equipo_activo_mapea_miembros_y_lider()
    {
        var lider = Guid.NewGuid();
        var miembro = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Los Halcones", lider);
        equipo.AgregarParticipante(miembro);
        var handler = new ObtenerMiEquipoQueryHandler(new FakeEquipoRepository { Activo = equipo });

        var result = await handler.Handle(new ObtenerMiEquipoQuery(lider), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(equipo.EquipoId, result!.EquipoId);
        Assert.Equal("Los Halcones", result.NombreEquipo);
        Assert.Equal("Activo", result.Estado);
        Assert.Equal(2, result.Participantes.Count);
        Assert.True(result.Participantes.Single(p => p.UsuarioId == lider).EsLider);
        Assert.False(result.Participantes.Single(p => p.UsuarioId == miembro).EsLider);
    }
}
```

- [ ] **Step 2: Correr y verificar que falla (a compilar)**

Run: `dotnet test services/identity-service/tests/Umbral.IdentityService.UnitTests/Umbral.IdentityService.UnitTests.csproj`
Expected: FAIL — no compila (`EquipoMineResponse`, `ObtenerMiEquipoQuery`, `ObtenerMiEquipoQueryHandler` no existen).

- [ ] **Step 3: Crear el DTO**

Crear `services/identity-service/src/Umbral.IdentityService.Application/DTOs/EquipoMineResponse.cs`:

```csharp
namespace Umbral.IdentityService.Application.DTOs;

public sealed record EquipoMineResponse(
    Guid EquipoId,
    string NombreEquipo,
    string Estado,
    IReadOnlyList<MiembroEquipoResponse> Participantes);

public sealed record MiembroEquipoResponse(Guid UsuarioId, bool EsLider);
```

- [ ] **Step 4: Crear la query**

Crear `services/identity-service/src/Umbral.IdentityService.Application/Queries/ObtenerMiEquipoQuery.cs`:

```csharp
using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Queries;

public sealed record ObtenerMiEquipoQuery(Guid ActorUserId) : IRequest<EquipoMineResponse?>;
```

- [ ] **Step 5: Crear el handler**

Crear `services/identity-service/src/Umbral.IdentityService.Application/Handlers/Queries/ObtenerMiEquipoQueryHandler.cs`:

```csharp
using MediatR;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Domain.Abstractions.Persistence;

namespace Umbral.IdentityService.Application.Handlers.Queries;

public sealed class ObtenerMiEquipoQueryHandler : IRequestHandler<ObtenerMiEquipoQuery, EquipoMineResponse?>
{
    private readonly IEquipoRepository _equipos;

    public ObtenerMiEquipoQueryHandler(IEquipoRepository equipos) => _equipos = equipos;

    public async Task<EquipoMineResponse?> Handle(ObtenerMiEquipoQuery request, CancellationToken cancellationToken)
    {
        var equipo = await _equipos.GetActiveByMemberUserIdAsync(request.ActorUserId, cancellationToken);
        if (equipo is null) return null;

        return new EquipoMineResponse(
            equipo.EquipoId,
            equipo.NombreEquipo,
            equipo.Estado.ToString(),
            equipo.Participantes
                .Select(p => new MiembroEquipoResponse(p.UsuarioId, p.EsLider))
                .ToList());
    }
}
```

- [ ] **Step 6: Correr y verificar verde**

Run: `dotnet test services/identity-service/tests/Umbral.IdentityService.UnitTests/Umbral.IdentityService.UnitTests.csproj`
Expected: PASS (los 2 nuevos + suite completa).

- [ ] **Step 7: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Application/DTOs/EquipoMineResponse.cs \
        services/identity-service/src/Umbral.IdentityService.Application/Queries/ObtenerMiEquipoQuery.cs \
        services/identity-service/src/Umbral.IdentityService.Application/Handlers/Queries/ObtenerMiEquipoQueryHandler.cs \
        services/identity-service/tests/Umbral.IdentityService.UnitTests/Teams/ObtenerMiEquipoQueryHandlerTests.cs
git commit -m "$(cat <<'EOF'
SP-3e-1a T1: ObtenerMiEquipoQuery + handler + EquipoMineResponse (read equipo activo del caller)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: Endpoint `GET /api/teams/mine` + contract tests + contrato

**Files:**
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Controllers/TeamsController.cs`
- Create: `services/identity-service/tests/Umbral.IdentityService.ContractTests/Teams/MiEquipoContractTests.cs`
- Modify: `contracts/http/identity-api.md`

**Interfaces:**
- Consumes: de Tarea 1 → `ObtenerMiEquipoQuery`, `EquipoMineResponse`. Existentes: `ISender _sender`, `AuthenticatedUserClaims.TryGetUserId`, `IdentityApiFactory.CreateClientAs("Participante", userId)`.
- Produces: acción `TeamsController.MiEquipo`.

- [ ] **Step 1: Escribir los contract tests (rojo)**

Crear `services/identity-service/tests/Umbral.IdentityService.ContractTests/Teams/MiEquipoContractTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Umbral.IdentityService.ContractTests.Teams;

/// <summary>Contract tests for GET /api/teams/mine (read del equipo activo del caller).</summary>
public sealed class MiEquipoContractTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public MiEquipoContractTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task MiEquipo_Returns200_WithShape_ForLeader()
    {
        var leaderId = Guid.NewGuid();
        await CreateTeamAsync(leaderId, "Mine Test Team");

        var client = _factory.CreateClientAs("Participante", leaderId);
        var response = await client.GetAsync("/api/teams/mine");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(doc.RootElement.TryGetProperty("equipoId", out _));
        Assert.True(doc.RootElement.TryGetProperty("nombreEquipo", out _));
        Assert.True(doc.RootElement.TryGetProperty("estado", out _));
        Assert.True(doc.RootElement.TryGetProperty("participantes", out var participantes));
        Assert.Equal(JsonValueKind.Array, participantes.ValueKind);
        var first = participantes[0];
        Assert.True(first.TryGetProperty("usuarioId", out _));
        Assert.True(first.TryGetProperty("esLider", out _));
    }

    [Fact]
    public async Task MiEquipo_Returns404_WhenNoActiveTeam()
    {
        var userId = Guid.NewGuid();
        var client = _factory.CreateClientAs("Participante", userId);

        var response = await client.GetAsync("/api/teams/mine");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MiEquipo_MemberSeesLeaderFlagFalse_ForThemselves()
    {
        var leaderId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        await CreateTeamAsync(leaderId, "Mine Member Flag Team");
        var invId = await InviteParticipantAsync(leaderId, memberId);
        await AcceptInvitationAsync(memberId, invId);

        var client = _factory.CreateClientAs("Participante", memberId);
        var response = await client.GetAsync("/api/teams/mine");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = doc.RootElement.GetProperty("participantes").EnumerateArray()
            .Single(p => Guid.Parse(p.GetProperty("usuarioId").GetString()!) == memberId);
        Assert.False(me.GetProperty("esLider").GetBoolean());
    }

    // ── Helpers (idénticos a InvitationsContractTests) ──
    private async Task<Guid> CreateTeamAsync(Guid leaderId, string name)
    {
        var client = _factory.CreateClientAs("Participante", leaderId);
        var response = await client.PostAsJsonAsync("/api/teams", new { nombreEquipo = name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return Guid.Parse(doc.RootElement.GetProperty("equipoId").GetString()!);
    }

    private async Task<Guid> InviteParticipantAsync(Guid leaderId, Guid invitadoId)
    {
        var leaderClient = _factory.CreateClientAs("Participante", leaderId);
        var response = await leaderClient.PostAsJsonAsync("/api/teams/invitations", new { invitadoUserId = invitadoId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return Guid.Parse(doc.RootElement.GetProperty("invitacionEquipoId").GetString()!);
    }

    private async Task AcceptInvitationAsync(Guid invitadoId, Guid invitacionId)
    {
        var client = _factory.CreateClientAs("Participante", invitadoId);
        var response = await client.PostAsJsonAsync($"/api/teams/invitations/{invitacionId}/acceptance", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test services/identity-service/tests/Umbral.IdentityService.ContractTests/Umbral.IdentityService.ContractTests.csproj`
Expected: FAIL — `GET /api/teams/mine` devuelve 404 de routing en los 3 tests (endpoint no existe aún). El de `Returns404_WhenNoActiveTeam` puede pasar accidentalmente por el 404 de routing; los otros dos fallan.

- [ ] **Step 3: Añadir el endpoint en `TeamsController.cs`**

Añadir el `using` de queries al inicio (junto a `using Umbral.IdentityService.Application.Commands;`):

```csharp
using Umbral.IdentityService.Application.Queries;
```

Añadir la acción dentro de la clase (p. ej. tras `Crear`):

```csharp
    [HttpGet("mine")]
    public async Task<IActionResult> MiEquipo(CancellationToken cancellationToken)
    {
        if (!AuthenticatedUserClaims.TryGetUserId(User, out var actorUserId))
            return Unauthorized();

        var equipo = await _sender.Send(new ObtenerMiEquipoQuery(actorUserId), cancellationToken);
        return equipo is null ? NotFound() : Ok(equipo);
    }
```

- [ ] **Step 4: Correr y verificar verde**

Run: `dotnet test services/identity-service/tests/Umbral.IdentityService.ContractTests/Umbral.IdentityService.ContractTests.csproj`
Expected: PASS (los 3 nuevos + suite completa).

- [ ] **Step 5: Documentar el contrato en `contracts/http/identity-api.md`**

En la tabla **### Teams and invitations (ParticipantOnly — role `Participante`)**, añadir esta fila tras `Get eligible participants`:

```markdown
| Get my active team | GET | `/api/teams/mine` | Registered | 200 `{ equipoId, nombreEquipo, estado, participantes:[{ usuarioId, esLider }] }`; 404 if caller has no active team |
```

- [ ] **Step 6: Commit (contrato + endpoint + tests)**

```bash
git add services/identity-service/src/Umbral.IdentityService.Api/Controllers/TeamsController.cs \
        services/identity-service/tests/Umbral.IdentityService.ContractTests/Teams/MiEquipoContractTests.cs \
        contracts/http/identity-api.md
git commit -m "$(cat <<'EOF'
SP-3e-1a T2: endpoint GET /api/teams/mine + contract tests + contrato identity-api

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 7: Verificar el carve-out intacto**

Run: `git status --short`
Expected: `docs/04-sdd/traceability-matrix.md` sigue `M` (sin commitear); `docs/superpowers/specs/2026-06-27-sp3c-runtime-trivia-design.md` y `docs/04-sdd/auditorias/` unstaged. Ningún archivo de docs commiteado.

---

## Self-Review

**1. Spec coverage (spec §3.1):**
- Query `ObtenerMiEquipoQuery` + handler → T1. ✅
- Endpoint `GET /api/teams/mine` (200 con equipo activo + miembros/líder; 404 sin equipo) → T2. ✅
- Contrato en `identity-api.md` → T2 step 5. ✅
- Tests: query handler (activo/sin equipo) + controller/contract (200/404/flag miembro) → T1 + T2. ✅
- Sin liderazgo forzado en el endpoint (devuelve el equipo de cualquier miembro) → T2 (no filtra por líder). ✅

**2. Placeholder scan:** sin TBD/TODO; todo step con código o comando concreto. ✅

**3. Type consistency:**
- `EquipoMineResponse(EquipoId, NombreEquipo, Estado, Participantes)` + `MiembroEquipoResponse(UsuarioId, EsLider)` idénticos en T1 (def), T2 (contract JSON `equipoId/nombreEquipo/estado/participantes/usuarioId/esLider`). ✅
- `ObtenerMiEquipoQuery(ActorUserId) : IRequest<EquipoMineResponse?>` idéntico en T1 (def), T2 (uso). ✅
- `IEquipoRepository.GetActiveByMemberUserIdAsync` usado tal cual existe (5 métodos implementados en el fake de T1). ✅

## Execution Handoff

Plan guardado en `docs/superpowers/plans/2026-07-01-sp3e1a-identity-mi-equipo.md`.
