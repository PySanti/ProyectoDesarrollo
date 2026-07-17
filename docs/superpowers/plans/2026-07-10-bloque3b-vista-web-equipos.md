# Bloque 3b — Vista web de equipos Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Listado web de solo lectura de todos los equipos (admin/operador) con enlace directo al rendimiento de equipo, respaldado por un endpoint nuevo `GET /identity/teams`.

**Architecture:** Identity gana una query de listado (repositorio → handler que enriquece miembros con nombres → controller nuevo autorizado por rol). El gateway añade una ruta método-GET con policy `OperadorOAdministrador` por delante de la ruta Participante existente. La web añade `EquiposPage` (área nav nueva "Equipos") y `RendimientoEquipoPage` aprende a precargar `?equipoId=`.

**Tech Stack:** .NET 8 + MediatR + EF Core (Identity), YARP (gateway), React 18 + Vite + TypeScript + vitest (web).

## Global Constraints

- Spec: `docs/superpowers/specs/2026-07-10-bloque3b-vista-web-equipos-design.md`.
- Solo lectura: ningún cambio a endpoints/contratos existentes; `POST /identity/teams`, `GET /identity/teams/mine`, membership y leadership quedan intactos.
- El endpoint nuevo lista TODOS los estados (`Activo`, `Desactivado`, `Eliminado`), ordenado por `nombreEquipo` ascendente; lista vacía → `200 []`.
- Payload por equipo: `{ equipoId, nombreEquipo, estado, participantes: [{ usuarioId, nombre, esLider }] }`. Usuario no encontrado al resolver nombre → `""`.
- Autorización: gateway policy `OperadorOAdministrador` (ya existe) en la ruta nueva; en Identity, policy nueva `OperadorOAdministrador` = `RequireRole("Operador", "Administrador")`.
- El controller nuevo NO puede vivir en `TeamsController` (su policy de clase `GestionarEquipos` es aditiva; admin/operador no tienen ese rol).
- Estructura Identity obligatoria: query en `Application/Queries/`, handler en `Application/Handlers/Queries/`, DTO en `Application/DTOs/`, controller en `Api/Controllers/` heredando `ControllerBase`, despacho por MediatR, tests de controller obligatorios.
- Web: no cambiar `label`/`id`/`data-testid`/ARIA existentes; reutilizar clases del design system (`page`, `card stack`, `table-wrap`, `notice error`, `muted`, `badge`).
- Commits terminan con `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- PROHIBIDO a subagentes: `git stash/reset/checkout/restore/clean`. Solo `git add <rutas exactas>` + `git commit`.
- Gates: Identity `dotnet test services/identity-service/Umbral.IdentityService.sln`; gateway `dotnet test gateway/Umbral.Gateway.sln`; web `npm test`, `npx tsc -b`, `npm run build` en `frontend/`.

---

### Task 1: Identity — `GET /identity/teams` (repositorio + query + handler + controller)

**Files:**
- Modify: `services/identity-service/src/Umbral.IdentityService.Domain/Abstractions/Persistence/IEquipoRepository.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Persistence/EquipoRepository.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/DTOs/EquipoAdminItemResponse.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Queries/ListarEquiposQuery.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Handlers/Queries/ListarEquiposQueryHandler.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Api/Controllers/TeamsAdminController.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Program.cs` (policy nueva)
- Test: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Teams/ListarEquiposQueryHandlerTests.cs` (create)
- Test: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Api/TeamsAdminControllerTests.cs` (create)
- Modify: los 9 fakes de `IEquipoRepository` en tests (lista abajo) — añadir el método nuevo.

**Interfaces:**
- Consumes: `IUsuarioRepository.GetAllAsync(CancellationToken)` → `IReadOnlyList<Usuario>` (ya existe; `Usuario.UsuarioId`, `Usuario.Nombre`).
- Produces: `GET identity/teams` → `200` con `IReadOnlyList<EquipoAdminItemResponse>`; serializado camelCase: `[{ equipoId, nombreEquipo, estado, participantes: [{ usuarioId, nombre, esLider }] }]`. Task 2 y Task 3 dependen de este shape exacto.

- [ ] **Step 1: Añadir `GetAllAsync` a la interfaz del repositorio**

En `IEquipoRepository.cs` añadir dentro de la interfaz:

```csharp
Task<IReadOnlyList<Equipo>> GetAllAsync(CancellationToken cancellationToken);
```

- [ ] **Step 2: Implementación EF**

En `EquipoRepository.cs` añadir (junto a los otros métodos de lectura):

```csharp
public async Task<IReadOnlyList<Equipo>> GetAllAsync(CancellationToken cancellationToken)
{
    return await _dbContext.Equipos
        .AsNoTracking()
        .Include(x => x.Participantes)
        .OrderBy(e => e.NombreEquipo)
        .ToListAsync(cancellationToken);
}
```

- [ ] **Step 3: Actualizar los 9 fakes de tests para que compilen**

Cada clase fake que implementa `IEquipoRepository` necesita el método nuevo. Añadir a cada una:

```csharp
public Task<IReadOnlyList<Equipo>> GetAllAsync(CancellationToken ct) =>
    Task.FromResult<IReadOnlyList<Equipo>>(Array.Empty<Equipo>());
```

Archivos (todos bajo `services/identity-service/tests/Umbral.IdentityService.UnitTests/`):
`CambiarRolUsuarioHandlerTests.cs`, `Teams/ObtenerMiEquipoQueryHandlerTests.cs`, `Teams/CrearEquipoHandlerTests.cs`, `Teams/SalirDeEquipoHandlerTests.cs`, `Teams/TransferirLiderazgoHandlerTests.cs`, `Teams/Invitations/GetInvitacionesRecibidasQueryHandlerTests.cs`, `Teams/Invitations/GetParticipantesElegiblesHandlerTests.cs`, `Teams/Invitations/AceptarInvitacionEquipoHandlerTests.cs`, `Teams/Invitations/EnviarInvitacionEquipoHandlerTests.cs`.

Si algún fake guarda una lista interna de equipos, devolver esa lista en vez del array vacío.

- [ ] **Step 4: DTO**

Crear `Application/DTOs/EquipoAdminItemResponse.cs`:

```csharp
namespace Umbral.IdentityService.Application.DTOs;

public sealed record EquipoAdminItemResponse(
    Guid EquipoId,
    string NombreEquipo,
    string Estado,
    IReadOnlyList<MiembroEquipoAdminResponse> Participantes);

public sealed record MiembroEquipoAdminResponse(Guid UsuarioId, string Nombre, bool EsLider);
```

- [ ] **Step 5: Query**

Crear `Application/Queries/ListarEquiposQuery.cs`:

```csharp
using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Queries;

public sealed record ListarEquiposQuery() : IRequest<IReadOnlyList<EquipoAdminItemResponse>>;
```

- [ ] **Step 6: Test del handler (falla: handler no existe)**

Crear `tests/Umbral.IdentityService.UnitTests/Teams/ListarEquiposQueryHandlerTests.cs`:

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
using Umbral.IdentityService.Domain.Enums;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Teams;

public class ListarEquiposQueryHandlerTests
{
    private sealed class FakeEquipoRepository : IEquipoRepository
    {
        public List<Equipo> Equipos = new();
        public Task<IReadOnlyList<Equipo>> GetAllAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Equipo>>(Equipos);
        public Task<Equipo?> GetActiveByMemberUserIdAsync(Guid userId, CancellationToken ct) =>
            Task.FromResult<Equipo?>(null);
        public Task<bool> ExistsActiveTeamByUserIdAsync(Guid userId, CancellationToken ct) =>
            Task.FromResult(false);
        public Task<Equipo?> GetByIdAsync(Guid equipoId, CancellationToken ct) =>
            Task.FromResult<Equipo?>(null);
        public Task AddAsync(Equipo equipo, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateAsync(Equipo equipo, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeUsuarioRepository : IUsuarioRepository
    {
        public List<Usuario> Usuarios = new();
        public Task<IReadOnlyList<Usuario>> GetAllAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Usuario>>(Usuarios);
        public Task<Usuario?> GetByIdAsync(Guid userId, CancellationToken ct) =>
            Task.FromResult<Usuario?>(null);
        public Task<bool> ExistsByEmailAsync(string email, Guid? excludingUserId, CancellationToken ct) =>
            Task.FromResult(false);
        public Task AddAsync(Usuario usuario, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateAsync(Usuario usuario, CancellationToken ct) => Task.CompletedTask;
        public Task RemoveAsync(Usuario usuario, CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    public async Task Sin_equipos_devuelve_lista_vacia()
    {
        var handler = new ListarEquiposQueryHandler(new FakeEquipoRepository(), new FakeUsuarioRepository());

        var result = await handler.Handle(new ListarEquiposQuery(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Mapea_equipo_con_nombres_de_miembros_y_lider()
    {
        var lider = Guid.NewGuid();
        var miembro = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Los Halcones", lider);
        equipo.AgregarParticipante(miembro);
        var usuarios = new FakeUsuarioRepository();
        usuarios.Usuarios.Add(Usuario.CrearDesdeKeycloak(lider, "kc-1", "Ana", "ana@umbral.test", RolUsuario.Participante));
        var handler = new ListarEquiposQueryHandler(
            new FakeEquipoRepository { Equipos = { equipo } }, usuarios);

        var result = await handler.Handle(new ListarEquiposQuery(), CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(equipo.EquipoId, item.EquipoId);
        Assert.Equal("Los Halcones", item.NombreEquipo);
        Assert.Equal("Activo", item.Estado);
        Assert.Equal(2, item.Participantes.Count);
        var pLider = item.Participantes.Single(p => p.UsuarioId == lider);
        Assert.Equal("Ana", pLider.Nombre);
        Assert.True(pLider.EsLider);
        // Usuario no registrado en la tabla local → nombre vacío, no explota.
        var pMiembro = item.Participantes.Single(p => p.UsuarioId == miembro);
        Assert.Equal("", pMiembro.Nombre);
        Assert.False(pMiembro.EsLider);
    }
}
```

Nota: si `Usuario.CrearDesdeKeycloak` no existe con esa firma, abrir `Domain/Entities/Usuario.cs` y usar la factory real que ya usan otros tests (buscar `Usuario.` en `tests/Umbral.IdentityService.UnitTests` para ver el patrón). No inventar shapes: usar la factory existente.

- [ ] **Step 7: Correr test — debe fallar por compilación (handler inexistente)**

Run: `dotnet test services/identity-service/tests/Umbral.IdentityService.UnitTests/Umbral.IdentityService.UnitTests.csproj --filter ListarEquipos`
Expected: FAIL (build error: `ListarEquiposQueryHandler` no existe).

- [ ] **Step 8: Handler**

Crear `Application/Handlers/Queries/ListarEquiposQueryHandler.cs`:

```csharp
using MediatR;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Domain.Abstractions.Persistence;

namespace Umbral.IdentityService.Application.Handlers.Queries;

public sealed class ListarEquiposQueryHandler
    : IRequestHandler<ListarEquiposQuery, IReadOnlyList<EquipoAdminItemResponse>>
{
    private readonly IEquipoRepository _equipos;
    private readonly IUsuarioRepository _usuarios;

    public ListarEquiposQueryHandler(IEquipoRepository equipos, IUsuarioRepository usuarios)
    {
        _equipos = equipos;
        _usuarios = usuarios;
    }

    public async Task<IReadOnlyList<EquipoAdminItemResponse>> Handle(
        ListarEquiposQuery request, CancellationToken cancellationToken)
    {
        var equipos = await _equipos.GetAllAsync(cancellationToken);
        var usuarios = await _usuarios.GetAllAsync(cancellationToken);
        var nombres = usuarios.ToDictionary(u => u.UsuarioId, u => u.Nombre);

        return equipos
            .Select(e => new EquipoAdminItemResponse(
                e.EquipoId,
                e.NombreEquipo,
                e.Estado.ToString(),
                e.Participantes
                    .Select(p => new MiembroEquipoAdminResponse(
                        p.UsuarioId,
                        nombres.TryGetValue(p.UsuarioId, out var nombre) ? nombre : "",
                        p.EsLider))
                    .ToList()))
            .ToList();
    }
}
```

- [ ] **Step 9: Correr tests del handler — PASS**

Run: `dotnet test services/identity-service/tests/Umbral.IdentityService.UnitTests/Umbral.IdentityService.UnitTests.csproj --filter ListarEquipos`
Expected: 2 PASS.

- [ ] **Step 10: Test del controller (falla: controller no existe)**

Crear `tests/Umbral.IdentityService.UnitTests/Api/TeamsAdminControllerTests.cs` (reutiliza el `FakeSender` interno existente del namespace `Umbral.IdentityService.UnitTests.Api`):

```csharp
using Microsoft.AspNetCore.Mvc;
using Umbral.IdentityService.Api.Controllers;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Queries;

namespace Umbral.IdentityService.UnitTests.Api;

public sealed class TeamsAdminControllerTests
{
    [Fact]
    public async Task Listar_Dispatches_Query_And_Returns_200_With_Payload()
    {
        var payload = new List<EquipoAdminItemResponse>
        {
            new(Guid.NewGuid(), "Equipo A", "Activo",
                new List<MiembroEquipoAdminResponse> { new(Guid.NewGuid(), "Ana", true) })
        };
        var sender = new FakeSender { NextResponse = payload };
        var controller = new TeamsAdminController(sender);

        var result = await controller.Listar(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(payload, ok.Value);
        Assert.IsType<ListarEquiposQuery>(sender.LastRequest);
    }
}
```

- [ ] **Step 11: Controller + policy**

Crear `Api/Controllers/TeamsAdminController.cs`:

```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.IdentityService.Application.Queries;

namespace Umbral.IdentityService.Api.Controllers;

// Listado de equipos para Administrador/Operador (vista web de solo lectura).
// Vive fuera de TeamsController porque la policy de clase GestionarEquipos es
// aditiva y esos roles no tienen ese permiso funcional.
[ApiController]
[Route("identity/teams")]
[Authorize(Policy = "OperadorOAdministrador")]
public sealed class TeamsAdminController : ControllerBase
{
    private readonly ISender _sender;

    public TeamsAdminController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var equipos = await _sender.Send(new ListarEquiposQuery(), cancellationToken);
        return Ok(equipos);
    }
}
```

En `Api/Program.cs`, dentro del bloque `AddAuthorization` existente (tiene `AdminOnly` y `GestionarEquipos`), añadir:

```csharp
options.AddPolicy("OperadorOAdministrador", policy => policy.RequireRole("Operador", "Administrador"));
```

- [ ] **Step 12: Suite completa de Identity — PASS**

Run: `dotnet test services/identity-service/Umbral.IdentityService.sln`
Expected: todo verde (antes de este bloque: 167 unit / 41 integration / 41 contract; ahora +3).

- [ ] **Step 13: Commit**

```bash
git add services/identity-service/src services/identity-service/tests
git commit -m "feat(identity): listado de equipos para admin/operador GET /identity/teams

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 2: Gateway — ruta `GET /identity/teams` con policy `OperadorOAdministrador`

**Files:**
- Modify: `gateway/src/Umbral.Gateway/appsettings.json` (bloque `Routes`)
- Test: `gateway/tests/Umbral.Gateway.IntegrationTests/GatewayEndpointsTests.cs` (añadir casos)

**Interfaces:**
- Consumes: endpoint de Task 1 (`GET identity/teams` en el servicio Identity).
- Produces: `GET http://localhost:5080/identity/teams` pasa con rol `Operador` o `Administrador`, 403 con solo `Participante`. `GET /identity/teams/mine` y `POST /identity/teams` siguen bajo policy `Participante`.

- [ ] **Step 1: Tests de la matriz de rutas (fallan)**

En `GatewayEndpointsTests.cs`, junto a los tests `IdentityTeams_*` existentes, añadir (mismo patrón `CreateClientWithRoles` / `AssertPolicyPassed` del archivo):

```csharp
[Fact]
public async Task IdentityTeamsListing_GET_con_Operador_pasa_la_politica()
{
    var client = CreateClientWithRoles("Operador");
    var response = await client.GetAsync("/identity/teams");
    AssertPolicyPassed(response);
}

[Fact]
public async Task IdentityTeamsListing_GET_con_Administrador_pasa_la_politica()
{
    var client = CreateClientWithRoles("Administrador");
    var response = await client.GetAsync("/identity/teams");
    AssertPolicyPassed(response);
}

[Fact]
public async Task IdentityTeamsListing_GET_con_Participante_es_403()
{
    // El listado es de la web (admin/operador); un participante puro no pasa.
    var client = CreateClientWithRoles("Participante");
    var response = await client.GetAsync("/identity/teams");
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}

[Fact]
public async Task IdentityTeamsListing_POST_sigue_siendo_de_Participante()
{
    // La ruta nueva solo matchea GET: crear equipo cae en la ruta Participante intacta.
    var client = CreateClientWithRoles("Participante");
    var response = await client.PostAsync("/identity/teams", new StringContent("{}"));
    AssertPolicyPassed(response);
}
```

- [ ] **Step 2: Correr tests — deben fallar**

Run: `dotnet test gateway/Umbral.Gateway.sln --filter IdentityTeamsListing`
Expected: FAIL — `GET /identity/teams` hoy matchea la ruta Participante (Operador/Administrador reciben 403; Participante pasa).

- [ ] **Step 3: Añadir la ruta en `appsettings.json`**

En el objeto `Routes`, inmediatamente ANTES de `"identity-teams"`, insertar:

```json
"identity-teams-listing": {
  "ClusterId": "identity",
  "Order": 0,
  "Match": { "Path": "/identity/teams", "Methods": [ "GET" ] },
  "AuthorizationPolicy": "OperadorOAdministrador"
},
```

No tocar nada más del archivo. La policy `OperadorOAdministrador` ya existe en `gateway/src/Umbral.Gateway/Program.cs:18`.

- [ ] **Step 4: Correr toda la suite del gateway — PASS**

Run: `dotnet test gateway/Umbral.Gateway.sln`
Expected: todo verde (17 existentes + 4 nuevos). Verificar en particular que `IdentityTeams_con_Participante_pasa_la_politica` (el de `/mine`) sigue verde.

- [ ] **Step 5: Commit**

```bash
git add gateway/src/Umbral.Gateway/appsettings.json gateway/tests/Umbral.Gateway.IntegrationTests/GatewayEndpointsTests.cs
git commit -m "feat(gateway): ruta GET /identity/teams para admin/operador

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 3: Web — `getEquipos` + `EquiposPage` + navegación

**Files:**
- Modify: `frontend/src/api/identityApi.ts` (+ `frontend/src/api/identityApi.test.ts`)
- Create: `frontend/src/features/identity/EquiposPage.tsx`
- Create: `frontend/src/features/identity/EquiposPage.test.tsx`
- Modify: `frontend/src/shell/navConfig.tsx` (+ `frontend/src/shell/navConfig.test.tsx`)
- Modify: `frontend/src/app/App.tsx` (+ `frontend/src/app/App.test.tsx`)

**Interfaces:**
- Consumes: `GET {VITE_GATEWAY_BASE_URL}/identity/teams` → `[{ equipoId, nombreEquipo, estado, participantes: [{ usuarioId, nombre, esLider }] }]` (Task 1/2). Helpers existentes del módulo: `resolveBaseUrl()`, `buildAuthHeaders()`, `parseJsonBody()`, `throwIfNotOk()`, `IdentityApiError`.
- Produces: `getEquipos(accessToken, fetchImpl?)` → `Promise<EquipoAdminItem[]>`; página en ruta `/equipos`; enlaces a `/puntuaciones/equipos?equipoId={id}` que Task 4 hace funcionales.

- [ ] **Step 1: Test de la API (falla)**

En `frontend/src/api/identityApi.test.ts`, siguiendo el patrón de los tests existentes de `getIdentityUsers` (mock de `fetch`, `import.meta.env` ya stubbeado en el archivo), añadir:

```ts
describe("getEquipos", () => {
  it("GET a /identity/teams con bearer y devuelve la lista", async () => {
    const equipos = [
      {
        equipoId: "e1",
        nombreEquipo: "Los Halcones",
        estado: "Activo",
        participantes: [{ usuarioId: "u1", nombre: "Ana", esLider: true }]
      }
    ];
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => equipos
    } as unknown as Response);

    const result = await getEquipos("tok", fetchMock as unknown as typeof fetch);

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining("/identity/teams"),
      expect.objectContaining({
        method: "GET",
        headers: expect.objectContaining({ Authorization: "Bearer tok" })
      })
    );
    expect(result).toEqual(equipos);
  });

  it("lanza IdentityApiError en error HTTP", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: false,
      status: 403,
      json: async () => ({ message: "prohibido" })
    } as unknown as Response);

    await expect(getEquipos("tok", fetchMock as unknown as typeof fetch)).rejects.toMatchObject({
      name: "IdentityApiError",
      statusCode: 403
    });
  });
});
```

Ajustar el import del test para incluir `getEquipos`. Si los tests existentes del archivo usan otro estilo de mock de fetch, copiar ese estilo exacto en lugar del de arriba.

- [ ] **Step 2: Correr — falla**

Run: `cd frontend && npx vitest run src/api/identityApi.test.ts`
Expected: FAIL (`getEquipos` no exportado).

- [ ] **Step 3: Implementar `getEquipos`**

Al final de `frontend/src/api/identityApi.ts`:

```ts
export interface EquipoMiembro {
  usuarioId: string;
  nombre: string;
  esLider: boolean;
}

export interface EquipoAdminItem {
  equipoId: string;
  nombreEquipo: string;
  estado: string;
  participantes: EquipoMiembro[];
}

export async function getEquipos(
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<EquipoAdminItem[]> {
  const response = await fetchImpl(`${resolveBaseUrl()}/identity/teams`, {
    method: "GET",
    headers: buildAuthHeaders(accessToken)
  });

  const body = await parseJsonBody<EquipoAdminItem[]>(response);
  throwIfNotOk(response, body as { message?: string });
  return body as EquipoAdminItem[];
}
```

Run: `npx vitest run src/api/identityApi.test.ts` → PASS.

- [ ] **Step 4: Test de `EquiposPage` (falla)**

Crear `frontend/src/features/identity/EquiposPage.test.tsx`. La página usa `Link` de react-router → envolver en `MemoryRouter` (mismo patrón que `frontend/src/features/partidas/HistorialPartidaPage.test.tsx`):

```tsx
import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { EquiposPage } from "./EquiposPage";
import * as identityApi from "../../api/identityApi";

const equipos: identityApi.EquipoAdminItem[] = [
  {
    equipoId: "11111111-2222-3333-4444-555555555555",
    nombreEquipo: "Los Halcones",
    estado: "Activo",
    participantes: [
      { usuarioId: "u1", nombre: "Ana", esLider: true },
      { usuarioId: "u2", nombre: "Luis", esLider: false }
    ]
  },
  {
    equipoId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
    nombreEquipo: "Zorros",
    estado: "Eliminado",
    participantes: [{ usuarioId: "u3", nombre: "Eva", esLider: true }]
  }
];

function renderPage() {
  return render(
    <MemoryRouter>
      <EquiposPage accessToken="tok" />
    </MemoryRouter>
  );
}

afterEach(() => vi.restoreAllMocks());

describe("EquiposPage", () => {
  it("muestra la tabla con miembros, líder marcado y badge de estado", async () => {
    vi.spyOn(identityApi, "getEquipos").mockResolvedValue(equipos);
    renderPage();
    expect(await screen.findByTestId("tabla-equipos")).toBeInTheDocument();
    expect(screen.getByText("Los Halcones")).toBeInTheDocument();
    expect(screen.getByText("Ana (líder), Luis")).toBeInTheDocument();
    expect(screen.getByText("Eliminado")).toBeInTheDocument();
  });

  it("cada fila enlaza al rendimiento con su equipoId", async () => {
    vi.spyOn(identityApi, "getEquipos").mockResolvedValue(equipos);
    renderPage();
    const links = await screen.findAllByRole("link", { name: "Ver rendimiento" });
    expect(links[0]).toHaveAttribute(
      "href",
      "/puntuaciones/equipos?equipoId=11111111-2222-3333-4444-555555555555"
    );
  });

  it("lista vacía muestra el mensaje de vacío", async () => {
    vi.spyOn(identityApi, "getEquipos").mockResolvedValue([]);
    renderPage();
    expect(await screen.findByText("No hay equipos registrados.")).toBeInTheDocument();
  });

  it("error de la api muestra aviso con reintento", async () => {
    vi.spyOn(identityApi, "getEquipos")
      .mockRejectedValueOnce(new identityApi.IdentityApiError("prohibido", 403))
      .mockResolvedValueOnce(equipos);
    renderPage();
    expect(await screen.findByText("prohibido")).toBeInTheDocument();
    (await screen.findByRole("button", { name: "Reintentar" })).click();
    expect(await screen.findByTestId("tabla-equipos")).toBeInTheDocument();
  });
});
```

Run: `npx vitest run src/features/identity/EquiposPage.test.tsx` → FAIL (módulo no existe).

- [ ] **Step 5: Implementar `EquiposPage`**

Crear `frontend/src/features/identity/EquiposPage.tsx`:

```tsx
// Vista de solo lectura de todos los equipos (admin/operador), con enlace
// directo al rendimiento histórico de cada equipo (bloque 3b).
import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getEquipos, IdentityApiError, type EquipoAdminItem } from "../../api/identityApi";

type Estado =
  | { status: "cargando" }
  | { status: "ok"; equipos: EquipoAdminItem[] }
  | { status: "error"; message: string };

function miembrosTexto(equipo: EquipoAdminItem): string {
  return equipo.participantes
    .map((p) => (p.esLider ? `${p.nombre} (líder)` : p.nombre))
    .join(", ");
}

export function EquiposPage({ accessToken }: { accessToken: string }) {
  const [estado, setEstado] = useState<Estado>({ status: "cargando" });

  const cargar = useCallback(async () => {
    setEstado({ status: "cargando" });
    try {
      const equipos = await getEquipos(accessToken);
      setEstado({ status: "ok", equipos });
    } catch (caught) {
      setEstado({
        status: "error",
        message:
          caught instanceof IdentityApiError
            ? caught.message
            : "Error inesperado al cargar los equipos."
      });
    }
  }, [accessToken]);

  useEffect(() => {
    void cargar();
  }, [cargar]);

  return (
    <div className="page" data-testid="equipos">
      <div className="card stack">
        <h1>Equipos</h1>
        {estado.status === "cargando" ? <p className="muted">Cargando…</p> : null}
        {estado.status === "error" ? (
          <>
            <div className="notice error" role="alert">
              {estado.message}
            </div>
            <button type="button" onClick={() => void cargar()}>
              Reintentar
            </button>
          </>
        ) : null}
        {estado.status === "ok" ? (
          estado.equipos.length === 0 ? (
            <p className="muted">No hay equipos registrados.</p>
          ) : (
            <div className="table-wrap">
              <table aria-label="Equipos" data-testid="tabla-equipos">
                <thead>
                  <tr>
                    <th scope="col">Nombre</th>
                    <th scope="col">Estado</th>
                    <th scope="col">Miembros</th>
                    <th scope="col">Rendimiento</th>
                  </tr>
                </thead>
                <tbody>
                  {estado.equipos.map((e) => (
                    <tr key={e.equipoId}>
                      <td>{e.nombreEquipo}</td>
                      <td>
                        <span className="badge">{e.estado}</span>
                      </td>
                      <td>{miembrosTexto(e)}</td>
                      <td>
                        <Link to={`/puntuaciones/equipos?equipoId=${e.equipoId}`}>
                          Ver rendimiento
                        </Link>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )
        ) : null}
      </div>
    </div>
  );
}
```

Antes de dar por bueno el markup: mirar cómo `UserManagementPage.tsx` renderiza badges (si usa `badge` con modificador por valor, p. ej. `badge ok`/`badge muted`, copiar ese patrón para Activo/Desactivado/Eliminado en lugar del `badge` pelado).

Run: `npx vitest run src/features/identity/EquiposPage.test.tsx` → PASS.

- [ ] **Step 6: Nav + ruta (tests primero)**

En `frontend/src/shell/navConfig.test.tsx`, dentro de `describe("areasForRoles")`, añadir:

```tsx
it("shows Equipos to both admin and operator", () => {
  expect(areasForRoles(["Administrador"]).map((a) => a.id)).toContain("equipos");
  expect(areasForRoles(["Operador"]).map((a) => a.id)).toContain("equipos");
});
```

En `frontend/src/app/App.test.tsx`, siguiendo el patrón exacto de `"allows an admin to reach team performance"` (pushState + render + findByRole heading):

```tsx
it("allows an operator to reach the teams list", async () => {
  window.history.pushState({}, "", "/equipos");
  renderAppAs("Operador"); // usar el helper real del archivo para montar con rol
  expect(await screen.findByRole("heading", { name: /equipos/i })).toBeInTheDocument();
});
```

(El helper de montaje con rol ya existe en el archivo — usar el que usan los tests vecinos, no inventar uno. Mockear `getEquipos` igual que esos tests mockean sus APIs si el montaje llega a llamar la red.)

Run: ambos archivos → FAIL.

- [ ] **Step 7: Implementar nav + ruta**

En `frontend/src/shell/navConfig.tsx`, añadir al final de `NAV_AREAS` (después del área `puntuaciones`):

```tsx
{
  id: "equipos",
  label: "Equipos",
  role: ["Operador", "Administrador"],
  icon: Users,
  items: [{ label: "Equipos", path: "/equipos", icon: Users }]
}
```

En `frontend/src/app/App.tsx`: importar `EquiposPage` y añadir la ruta junto a `puntuaciones/equipos`:

```tsx
{
  path: "equipos",
  element: (
    <RequireRole roles={roles} need={["Operador", "Administrador"]} landing={landing}>
      <EquiposPage accessToken={token} />
    </RequireRole>
  )
},
```

Run: `npx vitest run src/shell/navConfig.test.tsx src/app/App.test.tsx` → PASS.

- [ ] **Step 8: Gates completos del frontend**

Run (en `frontend/`): `npm test` && `npx tsc -b` && `npm run build`
Expected: suite completa verde, tsc limpio, build OK. Si `tsc -b` deja artefactos (`tsconfig*.tsbuildinfo`, `vite.config.js/d.ts`, `vitest.config.js/d.ts`), borrarlos — no commitearlos.

- [ ] **Step 9: Commit**

```bash
git add frontend/src/api/identityApi.ts frontend/src/api/identityApi.test.ts frontend/src/features/identity/EquiposPage.tsx frontend/src/features/identity/EquiposPage.test.tsx frontend/src/shell/navConfig.tsx frontend/src/shell/navConfig.test.tsx frontend/src/app/App.tsx frontend/src/app/App.test.tsx
git commit -m "feat(web): vista de equipos para admin/operador con enlace a rendimiento

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 4: Web — prefill `?equipoId=` en `RendimientoEquipoPage`

**Files:**
- Modify: `frontend/src/features/puntuaciones/RendimientoEquipoPage.tsx`
- Modify: `frontend/src/features/puntuaciones/RendimientoEquipoPage.test.tsx`

**Interfaces:**
- Consumes: enlaces `/puntuaciones/equipos?equipoId={guid}` generados por `EquiposPage` (Task 3); `getRendimientoEquipo(id, accessToken)` existente.
- Produces: al montar con `?equipoId=<guid válido>`, el campo queda precargado y la consulta se dispara sola. Sin query param → comportamiento actual intacto.

- [ ] **Step 1: Envolver los tests existentes en router y añadir los nuevos (fallan)**

`useSearchParams` exige contexto de router. En `RendimientoEquipoPage.test.tsx`:

1. Importar `MemoryRouter` y añadir un helper:

```tsx
import { MemoryRouter } from "react-router-dom";

function renderPage(initialEntry = "/puntuaciones/equipos") {
  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <RendimientoEquipoPage accessToken="tok" />
    </MemoryRouter>
  );
}
```

2. Reemplazar cada `render(<RendimientoEquipoPage accessToken="tok" />)` existente por `renderPage()`. No cambiar ninguna aserción existente.

3. Añadir:

```tsx
it("con ?equipoId= válido precarga el campo y consulta sola", async () => {
  vi.spyOn(puntuacionesApi, "getRendimientoEquipo").mockResolvedValue(rendimiento);
  renderPage(`/puntuaciones/equipos?equipoId=${GUID}`);
  expect(await screen.findByTestId("tabla-rendimiento")).toBeInTheDocument();
  expect(screen.getByLabelText("ID del equipo")).toHaveValue(GUID);
  expect(puntuacionesApi.getRendimientoEquipo).toHaveBeenCalledWith(GUID, "tok");
});

it("con ?equipoId= inválido no consulta y deja el flujo manual", () => {
  const spy = vi.spyOn(puntuacionesApi, "getRendimientoEquipo");
  renderPage("/puntuaciones/equipos?equipoId=no-es-guid");
  expect(spy).not.toHaveBeenCalled();
});
```

Run: `npx vitest run src/features/puntuaciones/RendimientoEquipoPage.test.tsx`
Expected: los 2 nuevos FAIL (la página ignora el query param); los existentes PASS ya envueltos.

- [ ] **Step 2: Implementar el prefill**

En `RendimientoEquipoPage.tsx`:

1. Actualizar el comentario de cabecera: quitar "Entrada por equipoId hasta que exista la vista web de equipos." y dejar: `// Entrada manual por equipoId o profunda vía ?equipoId= desde la vista de equipos (3b).`
2. Añadir imports: `useEffect`, `useRef` de react y `useSearchParams` de `react-router-dom`.
3. Extraer la consulta a una función reutilizable y disparo automático al montar:

```tsx
export function RendimientoEquipoPage({ accessToken }: { accessToken: string }) {
  const [searchParams] = useSearchParams();
  const [equipoId, setEquipoId] = useState("");
  const [formError, setFormError] = useState<string | null>(null);
  const [estado, setEstado] = useState<Estado>({ status: "inicial" });

  async function consultar(id: string) {
    setFormError(null);
    setEstado({ status: "cargando" });
    try {
      const rendimiento = await getRendimientoEquipo(id, accessToken);
      setEstado({ status: "ok", rendimiento });
    } catch (caught) {
      setEstado({
        status: "error",
        message:
          caught instanceof PuntuacionesApiError
            ? caught.message
            : "Error inesperado al consultar el rendimiento."
      });
    }
  }

  // Deep-link desde la vista de equipos: precarga y consulta una sola vez al montar.
  const autoConsultado = useRef(false);
  useEffect(() => {
    if (autoConsultado.current) return;
    autoConsultado.current = true;
    const fromQuery = searchParams.get("equipoId")?.trim() ?? "";
    if (GUID_RE.test(fromQuery)) {
      setEquipoId(fromQuery);
      void consultar(fromQuery);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function onConsultar(e: React.FormEvent) {
    e.preventDefault();
    const id = equipoId.trim();
    if (!GUID_RE.test(id)) {
      setFormError("Ingresa un ID de equipo válido (GUID).");
      setEstado({ status: "inicial" });
      return;
    }
    await consultar(id);
  }
  // ... resto del componente sin cambios
```

El JSX no cambia.

- [ ] **Step 3: Correr los tests del archivo — PASS**

Run: `npx vitest run src/features/puntuaciones/RendimientoEquipoPage.test.tsx`
Expected: todos PASS.

- [ ] **Step 4: Gates completos del frontend**

Run (en `frontend/`): `npm test` && `npx tsc -b` && `npm run build`
Expected: verde/limpio/OK. Borrar artefactos de `tsc -b` si aparecen (no commitearlos).

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/puntuaciones/RendimientoEquipoPage.tsx frontend/src/features/puntuaciones/RendimientoEquipoPage.test.tsx
git commit -m "feat(web): deep-link ?equipoId= en rendimiento de equipo

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 5: Gate E2E vía gateway (la corre el controlador de la sesión, no un subagente)

**Files:**
- Ninguno en el repo (scripts en el scratchpad de la sesión). Cierra con commit de docs si hay actualización de trazabilidad pendiente.

**Interfaces:**
- Consumes: todo lo anterior desplegado en el stack local (gateway :5080, identity :5000, Keycloak :8080 con usuarios seed `admin`/`admin`, operador y participante del realm).

Pasos (adaptar a los usuarios seed reales del realm — ver `infra/keycloak/README.md`):

- [ ] **Step 1:** Rebuild + restart de Identity y gateway (los binarios cambiaron): matar los procesos `dotnet` de identity y gateway, relanzar con sus `run-local.sh` (rutas absolutas).
- [ ] **Step 2:** Token participante (`get-token.sh` del scratchpad — recrearlo si el scratchpad se purgó, con `base64 -w0`). Si el participante no tiene equipo activo: `POST http://localhost:5080/identity/teams` con `{"nombreEquipo":"Equipo E2E 3b"}` → 201.
- [ ] **Step 3:** Token operador → `GET http://localhost:5080/identity/teams` → **200** con el equipo, miembros con `nombre` no vacío y `esLider` correcto.
- [ ] **Step 4:** Token admin → mismo GET → **200**.
- [ ] **Step 5:** Token participante → mismo GET → **403** (gateway).
- [ ] **Step 6:** Regresión: participante → `GET /identity/teams/mine` → 200/404 según tenga equipo (no 403).
- [ ] **Step 7:** Evidencia del deep-link ya cubierta por tests de Task 3/4 (href generado + auto-consulta); no requiere navegador.
- [ ] **Step 8:** Registrar evidencia en el ledger (`.git/sdd/progress.md`).

---

## Self-review (hecho)

- Cobertura del spec: contrato/orden/estados → T1; gateway → T2; página+nav+ruta → T3; prefill → T4; E2E → T5. Fuera de alcance respetado (sin escritura, sin mobile, sin detalle).
- Sin placeholders; código completo en cada step.
- Consistencia de tipos: `EquipoAdminItemResponse`/`MiembroEquipoAdminResponse` (C#) ↔ `EquipoAdminItem`/`EquipoMiembro` (TS) con casing camel en el wire; `getEquipos(accessToken, fetchImpl?)` usado igual en T3 página y tests; `ListarEquiposQuery()` sin parámetros en handler y controller.
