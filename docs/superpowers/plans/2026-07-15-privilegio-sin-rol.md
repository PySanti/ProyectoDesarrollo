# Privilegio sin rol — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Un usuario con `GestionarPartidas` o `GestionarEquipos` usa el panel web correspondiente sin importar su rol base (Administrador, Operador o Participante) — el privilegio autoriza, el rol no participa, en las tres capas que hoy todavía lo miran (nav web, gateway, backend).

**Architecture:** Se retira el filtro de rol base de las áreas de nav «Partidas»/«Equipos», de las policies compuestas del backend (Identity, Puntuaciones) que exigían rol AND privilegio, y de las rutas del gateway que exigían un rol fijo — las tres pasan a exigir sólo el privilegio, que ya viaja como role claim del token (ADR-0013). El área Identidad no se toca: sigue siendo exclusiva de Administrador.

**Tech Stack:** React 18 + Vite + TypeScript + Vitest (web); .NET 8 + xUnit (Identity, Puntuaciones, Partidas, Operaciones de Sesión, Gateway).

**Spec:** `docs/superpowers/specs/2026-07-15-privilegio-sin-rol-design.md`

## Global Constraints

- **Comentarios y documentación en español, con acentos correctos** ("está", "según", "así").
- **Nunca hacer push ni merge.** Commits locales en `feature/fixes-santiago`.
- **Prohibido `git stash`, `git reset`, `git restore`, `git clean`.** Sólo `git add <rutas exactas>` y `git commit`. Excepción: `git show <sha>:<ruta>` para leer código histórico.
- **Todo commit termina con:** `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`
- **Ledger:** tras cada tarea, una línea en `.git/sdd/progress.md`.
- **Regla del frontend del proyecto:** no cambiar `label`, `id`, `data-testid` ni roles ARIA de los que dependen los tests, salvo que la tarea lo pida.
- **`npx tsc --noEmit` sin `-p tsconfig.app.json` es un no-op** (sale 0 siempre, `tsconfig.json` tiene `"files": []`). Usar siempre `npx tsc --noEmit -p tsconfig.app.json`.
- **Paridad total (D2 del spec):** un Participante con el privilegio ve y opera exactamente lo mismo que Operador/Administrador con ese privilegio. Ninguna pantalla queda recortada para Participante.
- **Mobile no se toca (D3 del spec).** Ningún archivo bajo `mobile/` cambia en este plan.
- **El área Identidad no es un privilegio.** Sigue exclusiva del rol `Administrador`, sin cambios: `identidad/usuarios`, `identidad/usuarios/nuevo`, `identidad/gobernanza` en la web; policies `Administrador` en Identity; ruta `/identity/users` y `/identity/governance` en el gateway.
- **NO arrancar Docker fuera de la Task 6.** La verificación en vivo requiere autorización explícita del usuario.

## File Structure

| Archivo | Responsabilidad | Tarea |
|---|---|---|
| `frontend/src/shell/navConfig.tsx` | Las áreas Partidas/Equipos dejan de exigir rol base | 1 |
| `frontend/src/shell/states.tsx` | Copy de `UnauthorizedScreen` ya no dice "administradores y operadores" | 1 |
| `services/identity-service/.../Controllers/AdminTeamsController.cs`, `TeamsAdminController.cs` | Pasan a la policy `GestionarEquipos` (sólo-privilegio, ya existe) | 2 |
| `services/identity-service/.../Api/Program.cs` | Se borran las policies `AdminGestionarEquipos` / `OperadorOAdminGestionarEquipos` | 2 |
| `services/puntuaciones/.../Program.cs` | Nuevas policies `GestionarEquipos` / `GestionarPartidas` sólo-privilegio; se borran las AND | 3 |
| `services/puntuaciones/.../Controllers/EquiposController.cs`, `HistorialController.cs` | Pasan a las policies nuevas | 3 |
| `gateway/src/Umbral.Gateway/Program.cs` | Nuevas policies `GestionarPartidas` / `GestionarEquipos` | 4 |
| `gateway/src/Umbral.Gateway/appsettings.json` | 3 rutas cambian de policy | 4 |
| `CLAUDE.md` | Doctrina de gobernanza y regla de ruteo por cliente | 5 |

---

### Task 1: Web — el nav abre Partidas/Equipos por privilegio, sin importar el rol

**Files:**
- Modify: `frontend/src/shell/navConfig.tsx`
- Modify: `frontend/src/shell/states.tsx`
- Test: `frontend/src/shell/navConfig.test.tsx`
- Test: `frontend/src/app/App.test.tsx`

**Interfaces:**
- Produces: `NavAreaDef.role` pasa a **opcional** (`role?: Role | readonly Role[]`). `areasForRoles`/`landingPath` mantienen su firma actual.

- [ ] **Step 1: Escribir los tests que fallan**

Añadir a `frontend/src/shell/navConfig.test.tsx`, dentro de `describe("areasForRoles", ...)`:

```tsx
  /* Paridad total (D2 del spec de privilegio-sin-rol): el privilegio es el mismo permiso sin
     importar el rol base. Un Participante con GestionarPartidas o GestionarEquipos ve esas áreas
     igual que Operador/Administrador. */
  it("muestra Partidas y Equipos a un Participante con los privilegios, igual que a un operador", () => {
    const permisos = ["GestionarPartidas", "GestionarEquipos"];
    const areasParticipante = areasForRoles(["Participante"], permisos).map((a) => a.id);
    const areasOperador = areasForRoles(["Operador"], permisos).map((a) => a.id);

    expect(areasParticipante).toEqual(["partidas", "equipos"]);
    expect(areasOperador).toEqual(["partidas", "equipos"]);
  });

  it("sigue ocultando Identidad a un Participante, tenga o no privilegios", () => {
    const areas = areasForRoles(["Participante"], ["GestionarPartidas", "GestionarEquipos"]);

    expect(areas.map((a) => a.id)).not.toContain("identidad");
  });
```

Añadir a `frontend/src/app/App.test.tsx`, dentro de `describe("App shell + auth guard", ...)`:

```tsx
  /* Paridad total: un Participante con el privilegio entra al mismo panel que vería un Operador
     con ese privilegio — mismo mecanismo, D2 del spec de privilegio-sin-rol. */
  it("deja entrar a la creación de partidas a un participante con GestionarPartidas", async () => {
    vi.spyOn(partidasApi, "getPartidas").mockResolvedValue([]);
    window.history.pushState({}, "", "/partidas/crear");
    initMock.mockResolvedValueOnce({
      username: "participante",
      roles: ["Participante"],
      permisos: ["GestionarPartidas"],
      token: "token"
    });

    render(<App />);

    expect(await screen.findByRole("heading", { name: /crear partida/i })).toBeInTheDocument();
  });

  it("deja entrar a la creación de equipos a un participante con GestionarEquipos", async () => {
    window.history.pushState({}, "", "/identidad/equipos");
    initMock.mockResolvedValueOnce({
      username: "participante",
      roles: ["Participante"],
      permisos: ["GestionarEquipos"],
      token: "token"
    });

    render(<App />);

    expect(await screen.findByTestId("create-team-submit")).toBeInTheDocument();
  });
```

- [ ] **Step 2: Ejecutar y verificar que fallan**

Run: `cd frontend && npx vitest run src/shell/navConfig.test.tsx src/app/App.test.tsx`
Expected: los 4 tests nuevos FALLAN — `navConfig.tsx` sigue exigiendo `role: ["Operador", "Administrador"]` en las áreas Partidas/Equipos, así que un Participante nunca las ve.

- [ ] **Step 3: `NavAreaDef.role` pasa a opcional**

En `frontend/src/shell/navConfig.tsx`, cambiar la interfaz (línea 15-28):

```tsx
export interface NavAreaDef {
  id: string;
  label: string;
  /** Rol base requerido, si el área no se abre por privilegio (ej. Identidad). Las áreas
      gobernadas por permisos (Partidas, Equipos) no declaran esto: el privilegio decide solo,
      sin importar el rol — el privilegio autoriza, el rol no veta. */
  role?: Role | readonly Role[];
  icon: IconComponent;
  /** Privilegio que gobierna el área entera. Sin él, el área no existe para el usuario:
      ni en el menú ni por URL directa (ver `Require` en App.tsx). */
  permisos?: readonly string[];
  /** Dónde aterrizar si ésta es la primera área del usuario. Por defecto, su primer item.
      Se declara sólo cuando ese primero no es buen sitio para caer — un formulario vacío
      en vez de un listado, por ejemplo. */
  landing?: string;
  items: NavItemDef[];
}
```

- [ ] **Step 4: Sacar `role` de las áreas Partidas y Equipos**

En `NAV_AREAS` (líneas 46-75), quitar la línea `role: ["Operador", "Administrador"],` de ambas
áreas:

```tsx
  // GestionarPartidas gobierna el CRUD de partidas completo, consulta incluida. Sin `role`: el
  // privilegio decide solo, sin importar el rol base (privilegio-sin-rol).
  {
    id: "partidas",
    label: "Partidas",
    icon: Flag,
    permisos: ["GestionarPartidas"],
    items: [
      { label: "Partidas", path: "/partidas", icon: ListChecks },
      { label: "Nueva partida", path: "/partidas/crear", icon: Plus }
    ]
  },
  {
    id: "equipos",
    label: "Equipos",
    icon: Users,
    permisos: ["GestionarEquipos"],
    // Igual que Identidad: su primer item es un alta, no un listado.
    landing: "/equipos",
    items: [
      { label: "Creación de equipos", path: "/identidad/equipos", icon: Flag },
      { label: "Gestión de equipos", path: "/equipos", icon: Users },
      { label: "Rendimiento de equipos", path: "/puntuaciones/equipos", icon: ListChecks }
    ]
  }
```

El área `identidad` no cambia: conserva `role: "Administrador"`.

- [ ] **Step 5: Actualizar el filtro para tratar `role` ausente como "sin restricción de rol"**

Sustituir `areasForRoles` (líneas 78-95):

```tsx
/* El privilegio gobierna las áreas de gestión (Partidas, Equipos) sin importar el rol base: el
   privilegio autoriza, el rol no veta. `role` sólo restringe áreas que no son un privilegio, como
   Identidad — protegida y exclusiva de Administrador. */
export function areasForRoles(roles: string[], permisos: string[] = []): NavAreaDef[] {
  return NAV_AREAS.filter((area) => {
    if (area.role !== undefined) {
      const allowedRoles = typeof area.role === "string" ? [area.role] : area.role;
      if (!allowedRoles.some((role) => roles.includes(role))) {
        return false;
      }
    }
    return !area.permisos || area.permisos.some((permiso) => permisos.includes(permiso));
  }).map((area) => ({
    ...area,
    items: area.items.filter(
      (item) =>
        (!item.roles || item.roles.some((role) => roles.includes(role))) &&
        (!item.permisos || item.permisos.some((permiso) => permisos.includes(permiso)))
    )
  }));
}
```

- [ ] **Step 6: Ejecutar los tests de nav**

Run: `cd frontend && npx vitest run src/shell/navConfig.test.tsx`
Expected: PASS (todos, incluidos los 2 nuevos).

- [ ] **Step 7: Corregir el copy de `UnauthorizedScreen`**

En `frontend/src/shell/states.tsx`, el mensaje ya no es cierto (un Participante con privilegio sí
entra). Sustituir el `<h1>` y el `<p>` (líneas 79-85):

```tsx
        <h1 style={{ color: "var(--danger)" }}>Esta cuenta no tiene ningún panel disponible</h1>
        <p className="muted">
          La cuenta <strong>{username}</strong> no tiene ningún privilegio de gestión asignado.
          Pedile a un administrador que te asigne «Gestionar partidas» o «Gestionar equipos», o usa
          la app móvil para jugar.
        </p>
```

- [ ] **Step 8: Actualizar los tests de App.test.tsx que dependían del copy viejo**

En `frontend/src/app/App.test.tsx`, el primer test (líneas 40-53) prueba en realidad "sin ningún
privilegio", no "sin rol admin/operador" — renombrarlo y actualizar el regex:

```tsx
  it("muestra la pantalla de sin accesos a un participante sin ningún privilegio", async () => {
    initMock.mockResolvedValueOnce({
      username: "participante",
      roles: ["Participante"],
      permisos: [],
      token: "token"
    });

    render(<App />);

    expect(
      await screen.findByText(/esta cuenta no tiene ningún panel disponible/i)
    ).toBeInTheDocument();
  });
```

Y el test al final del archivo (líneas 326-339, "muestra la pantalla de sin accesos a un operador
sin ningún privilegio") actualiza sólo el regex:

```tsx
    expect(
      await screen.findByText(/esta cuenta no tiene ningún panel disponible/i)
    ).toBeInTheDocument();
```

- [ ] **Step 9: Ejecutar toda la suite y el typecheck**

Run: `cd frontend && npm test && npx tsc --noEmit -p tsconfig.app.json`
Expected: PASS, typecheck limpio.

> ⚠️ **`-p tsconfig.app.json` no es opcional** — sin él, `tsc` no mira ningún archivo y sale 0
> siempre haya o no errores.

- [ ] **Step 10: Commit**

```bash
git add frontend/src/shell/navConfig.tsx frontend/src/shell/navConfig.test.tsx frontend/src/shell/states.tsx frontend/src/app/App.test.tsx
git commit -m "feat(web): el privilegio abre Partidas y Equipos sin importar el rol base

Las areas Partidas y Equipos exigian ademas del privilegio un rol
Operador/Administrador. Un Participante con el privilegio, aunque el
backend ya lo autorizara, nunca llegaba a verlas. Se quita el filtro de
rol: NavAreaDef.role pasa a opcional y solo Identidad (protegida,
exclusiva de Administrador) lo sigue usando.

El copy de la pantalla de sin acceso decia 'exclusivo para
administradores y operadores', que ya no es cierto. Pasa a explicar la
regla real: sin ningun privilegio de gestion asignado, no hay panel.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

- [ ] **Step 11: Ledger**

```bash
echo "2026-07-15 | HU-04 | s3-task 1 | nav web abre Partidas/Equipos por privilegio, sin rol. commit <sha>. frontend N/N verde, tsc limpio." >> .git/sdd/progress.md
```

---

### Task 2: Backend Identity — las policies de equipos pierden el rol

**Files:**
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Program.cs:117-135`
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Controllers/AdminTeamsController.cs:13`
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Controllers/TeamsAdminController.cs:8-15`
- Test: `services/identity-service/tests/Umbral.IdentityService.ContractTests/Teams/AdminGestionEquiposContractTests.cs`

**Interfaces:**
- Consumes: la policy `GestionarEquipos` que ya existe en `Program.cs` (`RequireRole("GestionarEquipos")`, sólo-privilegio, hoy sin ningún `[Authorize]` que la use).
- Produces: `AdminTeamsController` y `TeamsAdminController` autorizan con esa misma policy. Se borran `AdminGestionarEquipos` y `OperadorOAdminGestionarEquipos`.

- [ ] **Step 1: Reescribir los tests del AND — ahora prueban que el rol ya no importa**

Reemplazar el contenido completo de
`services/identity-service/tests/Umbral.IdentityService.ContractTests/Teams/AdminGestionEquiposContractTests.cs`:

```csharp
using System.Net;

namespace Umbral.IdentityService.ContractTests.Teams;

/// <summary>
/// Privilegio-sin-rol: AdminTeamsController y TeamsAdminController exigen solo GestionarEquipos.
/// El rol base ya no participa — ni delimita, ni veta. Dos casos por endpoint: sin el privilegio,
/// cualquier rol (incluido Administrador) es 403; con el privilegio, cualquier rol (incluido
/// Participante) pasa.
/// </summary>
public sealed class AdminGestionEquiposContractTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public AdminGestionEquiposContractTests(IdentityApiFactory factory) => _factory = factory;

    // ── AdminTeamsController (/identity/admin/teams) — policy GestionarEquipos ──────────

    [Fact]
    public async Task AdminTeams_Administrador_sin_privilegio_es_403()
    {
        var client = _factory.CreateClientWithRoles(Guid.NewGuid(), "Administrador");

        var response = await client.GetAsync("/identity/admin/teams");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminTeams_Participante_con_privilegio_pasa()
    {
        // El caso que antes era 403 por el AND de rol: ahora el privilegio solo alcanza.
        var client = _factory.CreateClientWithRoles(Guid.NewGuid(), "Participante", "GestionarEquipos");

        var response = await client.GetAsync("/identity/admin/teams");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminTeams_Administrador_con_privilegio_pasa()
    {
        var client = _factory.CreateClientWithRoles(Guid.NewGuid(), "Administrador", "GestionarEquipos");

        var response = await client.GetAsync("/identity/admin/teams");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── TeamsAdminController (/identity/teams) — policy GestionarEquipos ───────

    [Fact]
    public async Task TeamsAdmin_Operador_sin_privilegio_es_403()
    {
        var client = _factory.CreateClientWithRoles(Guid.NewGuid(), "Operador");

        var response = await client.GetAsync("/identity/teams");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TeamsAdmin_Participante_con_privilegio_pasa()
    {
        var client = _factory.CreateClientWithRoles(Guid.NewGuid(), "Participante", "GestionarEquipos");

        var response = await client.GetAsync("/identity/teams");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TeamsAdmin_Operador_con_privilegio_pasa()
    {
        var client = _factory.CreateClientWithRoles(Guid.NewGuid(), "Operador", "GestionarEquipos");

        var response = await client.GetAsync("/identity/teams");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TeamsAdmin_Administrador_con_privilegio_pasa()
    {
        // Con los defaults reales el Administrador ya trae GestionarEquipos vía CreateClientAs.
        var client = _factory.CreateClientAs("Administrador", Guid.NewGuid());

        var response = await client.GetAsync("/identity/teams");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

- [ ] **Step 2: Ejecutar y verificar que fallan**

Run: `dotnet test services/identity-service/tests/Umbral.IdentityService.ContractTests/Umbral.IdentityService.ContractTests.csproj --filter AdminGestionEquiposContractTests`
Expected: FAIL en `AdminTeams_Participante_con_privilegio_pasa` y `TeamsAdmin_Participante_con_privilegio_pasa` — las policies actuales (`AdminGestionarEquipos`, `OperadorOAdminGestionarEquipos`) exigen además del privilegio un rol que un Participante no tiene, así que ambos casos dan 403 en vez del 200 esperado. Los demás tests ya pasan hoy (no cambian de resultado, sólo quedan como regresión-pin de lo que ya era cierto).

- [ ] **Step 3: `AdminTeamsController` pasa a la policy `GestionarEquipos`**

En `services/identity-service/src/Umbral.IdentityService.Api/Controllers/AdminTeamsController.cs:13`:

```csharp
[Authorize(Policy = "GestionarEquipos")]
```

- [ ] **Step 4: `TeamsAdminController` pasa a la policy `GestionarEquipos` y su comentario se corrige**

En `services/identity-service/src/Umbral.IdentityService.Api/Controllers/TeamsAdminController.cs:8-15`:

```csharp
// Listado de equipos para quien tenga GestionarEquipos (vista web de solo lectura). Vive fuera de
// TeamsController porque administra equipos ajenos: TeamsController es para el equipo propio del
// Participante (viene con el rol), esto es para cualquiera con el privilegio de gestión.
[ApiController]
[Route("identity/teams")]
[Authorize(Policy = "GestionarEquipos")]
public sealed class TeamsAdminController : ControllerBase
```

- [ ] **Step 5: Borrar las policies AND, que quedan sin uso**

En `services/identity-service/src/Umbral.IdentityService.Api/Program.cs`, dentro de
`AddAuthorization` (líneas 117-135), borrar el bloque de las dos policies compuestas:

```csharp
    // Los privilegios son aditivos, no sustitutivos: el panel puede dar GestionarEquipos a cualquier
    // rol. El rol delimita el ámbito y el privilegio habilita el CRUD, y se exigen los dos porque los
    // puertos de servicio están expuestos y una policy de sólo-privilegio se saltaría el filtro por
    // rol del gateway.
    options.AddPolicy("AdminGestionarEquipos", policy =>
        policy.RequireRole("Administrador").RequireRole("GestionarEquipos"));
    options.AddPolicy("OperadorOAdminGestionarEquipos", policy =>
        policy.RequireRole("Operador", "Administrador").RequireRole("GestionarEquipos"));
```

Ese bloque desaparece por completo — no queda ningún `[Authorize]` que lo referencie tras el Step 3
y el Step 4. La policy `GestionarEquipos` que sí queda (línea 120 antes del borrado) no cambia.

- [ ] **Step 6: Ejecutar las 3 suites de Identity**

Run: `dotnet test services/identity-service/Umbral.IdentityService.sln`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Api/Program.cs services/identity-service/src/Umbral.IdentityService.Api/Controllers/AdminTeamsController.cs services/identity-service/src/Umbral.IdentityService.Api/Controllers/TeamsAdminController.cs services/identity-service/tests/Umbral.IdentityService.ContractTests/Teams/AdminGestionEquiposContractTests.cs
git commit -m "feat(identity): las policies de equipos pierden el AND de rol

AdminTeamsController y TeamsAdminController exigian rol (Administrador /
Operador-o-Administrador) Y GestionarEquipos. Con la gobernanza abierta a
que un Participante tenga GestionarEquipos, ese AND lo dejaba afuera sin
poder usarlo nunca. Pasan a la policy GestionarEquipos que ya existia sin
uso (solo-privilegio); se borran las dos policies AND, que quedan sin
ningun Authorize que las referencie.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

- [ ] **Step 8: Ledger**

```bash
echo "2026-07-15 | HU-04 | s3-task 2 | Identity: AdminTeamsController/TeamsAdminController a policy GestionarEquipos solo-privilegio, se borran las 2 policies AND. commit <sha>. Identity N/N verde." >> .git/sdd/progress.md
```

---

### Task 3: Backend Puntuaciones — nuevas policies sólo-privilegio

**Files:**
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Program.cs:134-144`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Controllers/EquiposController.cs:10`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Controllers/HistorialController.cs:8-12`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/AutorizacionContractTests.cs`

**Interfaces:**
- Produces: policies `GestionarEquipos` y `GestionarPartidas` (sólo-privilegio) en Puntuaciones —
  mismo nombre y mecanismo que ya usan Partidas y Operaciones de Sesión.

- [ ] **Step 1: Reescribir los tests del AND**

En `services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/AutorizacionContractTests.cs`,
sustituir el bloque completo desde el comentario `// Task 5: EquiposController...` (línea 48) hasta
el final de la clase (línea 116), por:

```csharp
    // Privilegio-sin-rol: EquiposController y HistorialController exigen solo el privilegio. El rol
    // base ya no participa — sin él, cualquier rol (incluido Administrador) es 403; con él,
    // cualquier rol (incluido Participante) pasa.

    [Fact]
    public async Task Equipos_rendimiento_sin_privilegio_es_403()
    {
        var client = _factory.CreateClientConRoles("Administrador");

        var response = await client.GetAsync($"/puntuaciones/equipos/{Guid.NewGuid()}/rendimiento");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Equipos_rendimiento_Participante_con_privilegio_no_es_401_ni_403()
    {
        var client = _factory.CreateClientConRoles("Participante", "GestionarEquipos");

        var response = await client.GetAsync($"/puntuaciones/equipos/{Guid.NewGuid()}/rendimiento");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Equipos_rendimiento_Operador_con_privilegio_no_es_401_ni_403()
    {
        var client = _factory.CreateClientConRoles("Operador", "GestionarEquipos");

        var response = await client.GetAsync($"/puntuaciones/equipos/{Guid.NewGuid()}/rendimiento");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Historial_sin_privilegio_es_403()
    {
        var client = _factory.CreateClientConRoles("Administrador");

        var response = await client.GetAsync($"/puntuaciones/partidas/{Guid.NewGuid()}/historial");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Historial_Participante_con_privilegio_no_es_401_ni_403()
    {
        var client = _factory.CreateClientConRoles("Participante", "GestionarPartidas");

        var response = await client.GetAsync($"/puntuaciones/partidas/{Guid.NewGuid()}/historial");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Historial_Operador_con_privilegio_no_es_401_ni_403()
    {
        var client = _factory.CreateClientConRoles("Operador", "GestionarPartidas");

        var response = await client.GetAsync($"/puntuaciones/partidas/{Guid.NewGuid()}/historial");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
```

- [ ] **Step 2: Ejecutar y verificar que fallan**

Run: `dotnet test services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/Umbral.Puntuaciones.ContractTests.csproj --filter AutorizacionContractTests`
Expected: FAIL en `Equipos_rendimiento_Participante_con_privilegio_no_es_401_ni_403` y
`Historial_Participante_con_privilegio_no_es_401_ni_403` (las policies actuales exigen
Operador/Administrador, así que un Participante recibe 403).

- [ ] **Step 3: Agregar las policies sólo-privilegio y borrar las AND**

En `services/puntuaciones/src/Umbral.Puntuaciones.Api/Program.cs`, sustituir el bloque
`AddAuthorization` (líneas 134-144):

```csharp
builder.Services.AddAuthorization(options =>
{
    // Privilegio-sin-rol: el rol base no participa. Mismo patron que Partidas y Operaciones de
    // Sesion — el privilegio es un role claim del token (ADR-0013), asi que RequireRole lo lee
    // igual que un rol base.
    options.AddPolicy("GestionarEquipos", p => p.RequireRole("GestionarEquipos"));
    options.AddPolicy("GestionarPartidas", p => p.RequireRole("GestionarPartidas"));
});
```

- [ ] **Step 4: `EquiposController` pasa a `GestionarEquipos`**

En `services/puntuaciones/src/Umbral.Puntuaciones.Api/Controllers/EquiposController.cs:10`:

```csharp
[Authorize(Policy = "GestionarEquipos")]
```

- [ ] **Step 5: `HistorialController` pasa a `GestionarPartidas` y su comentario se corrige**

En `services/puntuaciones/src/Umbral.Puntuaciones.Api/Controllers/HistorialController.cs:8-12`:

```csharp
// HU-43: el historial expone respuestas, pistas y ubicaciones de todos los participantes —
// autorizado por el privilegio GestionarPartidas, sin importar el rol de quien lo tenga.
[ApiController]
[Route("puntuaciones")]
[Authorize(Policy = "GestionarPartidas")]
public sealed class HistorialController : ControllerBase
```

- [ ] **Step 6: Ejecutar las 3 suites de Puntuaciones**

Run: `dotnet test services/puntuaciones/Umbral.Puntuaciones.sln`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add services/puntuaciones/src/Umbral.Puntuaciones.Api/Program.cs services/puntuaciones/src/Umbral.Puntuaciones.Api/Controllers/EquiposController.cs services/puntuaciones/src/Umbral.Puntuaciones.Api/Controllers/HistorialController.cs services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/AutorizacionContractTests.cs
git commit -m "feat(puntuaciones): EquiposController y HistorialController a policies solo-privilegio

Exigian rol (Operador-o-Administrador) Y el privilegio. Con la
gobernanza abierta a que un Participante tenga GestionarEquipos o
GestionarPartidas, ese AND lo dejaba afuera. Nuevas policies
GestionarEquipos/GestionarPartidas solo-privilegio, mismo patron que ya
usan Partidas y Operaciones de Sesion; se borran las 2 policies AND.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

- [ ] **Step 8: Ledger**

```bash
echo "2026-07-15 | HU-04 | s3-task 3 | Puntuaciones: EquiposController/HistorialController a policies GestionarEquipos/GestionarPartidas solo-privilegio, se borran las 2 policies AND. commit <sha>. Puntuaciones N/N verde." >> .git/sdd/progress.md
```

---

### Task 4: Gateway — las rutas pasan de rol base a privilegio

**Files:**
- Modify: `gateway/src/Umbral.Gateway/Program.cs:13-19`
- Modify: `gateway/src/Umbral.Gateway/appsettings.json`
- Test: `gateway/tests/Umbral.Gateway.IntegrationTests/GatewayEndpointsTests.cs`

**Interfaces:**
- Produces: policies `GestionarPartidas` y `GestionarEquipos` en el gateway
  (`RequireRole(privilegio)`), sumadas a las 4 de rol base que ya existen.

- [ ] **Step 1: Actualizar los tests existentes que hoy pasan con solo el rol**

En `gateway/tests/Umbral.Gateway.IntegrationTests/GatewayEndpointsTests.cs`, estos tests van a
empezar a fallar porque hoy pasan un rol sin privilegio y la policy nueva no mira el rol:

Reemplazar `Partidas_con_Administrador_pasa_la_politica` y `Partidas_con_Operador_pasa_la_politica`
(líneas 202-216) por:

```csharp
    [Fact]
    public async Task Partidas_con_Administrador_sin_privilegio_es_403()
    {
        var client = CreateClientWithRoles("Administrador");
        var response = await client.GetAsync("/partidas/anything");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Partidas_con_Participante_con_GestionarPartidas_pasa_la_politica()
    {
        // El caso nuevo: privilegio-sin-rol, un Participante con el privilegio ya no es 403.
        var client = CreateClientWithRoles("Participante,GestionarPartidas");
        var response = await client.GetAsync("/partidas/anything");
        AssertPolicyPassed(response);
    }

    [Fact]
    public async Task Partidas_con_Operador_con_GestionarPartidas_pasa_la_politica()
    {
        var client = CreateClientWithRoles("Operador,GestionarPartidas");
        var response = await client.GetAsync("/partidas/anything");
        AssertPolicyPassed(response);
    }
```

Y `Partidas_con_Participante_es_403` (línea 194-200) queda igual — bare `Participante` sin
privilegio sigue siendo 403, sólo que ahora por falta de privilegio en vez de rol equivocado; no
hace falta tocarla.

Reemplazar `IdentityAdminTeams_con_Administrador_pasa_la_politica` (línea 298-304) por:

```csharp
    [Fact]
    public async Task IdentityAdminTeams_con_Administrador_sin_privilegio_es_403()
    {
        var client = CreateClientWithRoles("Administrador");
        var response = await client.GetAsync("/identity/admin/teams/cualquier-cosa");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task IdentityAdminTeams_con_Participante_con_GestionarEquipos_pasa_la_politica()
    {
        var client = CreateClientWithRoles("Participante,GestionarEquipos");
        var response = await client.GetAsync("/identity/admin/teams/cualquier-cosa");
        AssertPolicyPassed(response);
    }
```

`IdentityAdminTeams_con_Participante_es_403` y `IdentityAdminTeams_con_Operador_es_403` (líneas
280-296) quedan igual — bare, sin privilegio, siguen en 403.

Reemplazar `IdentityTeamsListing_GET_con_Operador_pasa_la_politica` y
`IdentityTeamsListing_GET_con_Administrador_pasa_la_politica` (líneas 152-166) por:

```csharp
    [Fact]
    public async Task IdentityTeamsListing_GET_con_Operador_con_GestionarEquipos_pasa_la_politica()
    {
        var client = CreateClientWithRoles("Operador,GestionarEquipos");
        var response = await client.GetAsync("/identity/teams");
        AssertPolicyPassed(response);
    }

    [Fact]
    public async Task IdentityTeamsListing_GET_con_Participante_con_GestionarEquipos_pasa_la_politica()
    {
        // El caso nuevo: antes esta misma ruta, con el mismo Participante, era 403 (test de abajo).
        var client = CreateClientWithRoles("Participante,GestionarEquipos");
        var response = await client.GetAsync("/identity/teams");
        AssertPolicyPassed(response);
    }
```

`IdentityTeamsListing_GET_con_Participante_es_403` (línea 168-175) queda igual — bare, sin
privilegio, sigue en 403; su comentario ("El listado es de la web (admin/operador); un participante
puro no pasa") sigue siendo cierto para un participante *sin* privilegio.

- [ ] **Step 2: Ejecutar y verificar que fallan**

Run: `dotnet test gateway/tests/Umbral.Gateway.IntegrationTests/Umbral.Gateway.IntegrationTests.csproj`
Expected: FAIL en los tests `_sin_privilegio_es_403` (las rutas siguen en
`OperadorOAdministrador`/`Administrador`, que hoy dejan pasar el rol solo, sin privilegio) y en los
tests que le agregan el privilegio a un **Participante** (`Partidas_con_Participante_con_
GestionarPartidas_pasa_la_politica`, `IdentityAdminTeams_con_Participante_con_GestionarEquipos_
pasa_la_politica`, `IdentityTeamsListing_GET_con_Participante_con_GestionarEquipos_pasa_la_
politica`) — las policies actuales no conocen a Participante para estas rutas, así que siguen en
403. Los tests que combinan Operador/Administrador con el privilegio ya pasaban hoy (el rol solo ya
alcanzaba) y siguen pasando — quedan como pin de regresión, no como caso nuevo.

- [ ] **Step 3: Agregar las policies de privilegio**

En `gateway/src/Umbral.Gateway/Program.cs:13-19`:

```csharp
// AuthZ: los tres roles base + los dos privilegios gobernables (tambien role claims del token,
// ADR-0013) + secure-by-default fallback.
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Administrador", p => p.RequireRole("Administrador"))
    .AddPolicy("Operador", p => p.RequireRole("Operador"))
    .AddPolicy("Participante", p => p.RequireRole("Participante"))
    .AddPolicy("OperadorOAdministrador", p => p.RequireRole("Operador", "Administrador"))
    .AddPolicy("GestionarPartidas", p => p.RequireRole("GestionarPartidas"))
    .AddPolicy("GestionarEquipos", p => p.RequireRole("GestionarEquipos"))
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
```

- [ ] **Step 4: Cambiar las 3 rutas afectadas en `appsettings.json`**

En `gateway/src/Umbral.Gateway/appsettings.json`:

```json
      "identity-admin-teams": {
        "ClusterId": "identity",
        "Order": 1,
        "Match": { "Path": "/identity/admin/teams/{**catch-all}" },
        "AuthorizationPolicy": "GestionarEquipos"
      },
      "identity-teams-listing": {
        "ClusterId": "identity",
        "Order": 0,
        "Match": { "Path": "/identity/teams", "Methods": [ "GET" ] },
        "AuthorizationPolicy": "GestionarEquipos"
      },
```

```json
      "partidas": {
        "ClusterId": "partidas",
        "Match": { "Path": "/partidas/{**catch-all}" },
        "AuthorizationPolicy": "GestionarPartidas"
      },
```

El resto de `appsettings.json` no cambia (`identity-governance`, `identity-users`,
`identity-teams` no-GET, `identity` catch-all, `operaciones-sesion`, `puntuaciones`).

- [ ] **Step 5: Ejecutar toda la suite del gateway**

Run: `dotnet test gateway/tests/Umbral.Gateway.IntegrationTests/Umbral.Gateway.IntegrationTests.csproj`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add gateway/src/Umbral.Gateway/Program.cs gateway/src/Umbral.Gateway/appsettings.json gateway/tests/Umbral.Gateway.IntegrationTests/GatewayEndpointsTests.cs
git commit -m "feat(gateway): /partidas y /identity/admin/teams pasan de rol a privilegio

Las 3 rutas exigian un rol base fijo (Operador-o-Administrador,
Administrador). Con el backend ya autorizando solo por privilegio
(tasks 2 y 3), estas rutas seguian cortando en el borde a cualquiera sin
ese rol, incluido un Participante con el privilegio real. Pasan a
RequireRole(privilegio) — mismo mecanismo que ya usa el gateway para el
rol base, ya que el privilegio tambien es un role claim del token
(ADR-0013). Sigue habiendo defensa en profundidad: gateway y backend
verifican el mismo privilegio, ahora consistentes entre si.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

- [ ] **Step 7: Ledger**

```bash
echo "2026-07-15 | HU-04 | s3-task 4 | gateway: /partidas, /identity/admin/teams, /identity/teams GET pasan de rol a RequireRole(privilegio). commit <sha>. gateway N/N verde." >> .git/sdd/progress.md
```

---

### Task 5: Documentación — CLAUDE.md refleja privilegio-sin-rol

**Files:**
- Modify: `CLAUDE.md`

- [ ] **Step 1: Corregir "Roles, permissions & governance"**

Buscar esta frase (sección "Roles, permissions & governance"):

```
- **The panel governs exactly two privileges: `GestionarPartidas` and `GestionarEquipos`.** Each opens its whole area in whichever client the role uses. `GestionarEquipos` governs only the **web panels for administering other people's teams** — a participant's own team (create, invite, lead, leave) comes with the `Participante` role.
```

Sustituir por:

```
- **The panel governs exactly two privileges: `GestionarPartidas` and `GestionarEquipos`.** Each always opens its whole area on **web**, regardless of the holder's base role — a Participante who receives either privilege uses the same web panel an Operador/Administrador would, not a mobile equivalent. `GestionarEquipos` governs only the **web panels for administering other people's teams** — a participant's own team (create, invite, lead, leave) comes with the `Participante` role and stays on mobile.
```

- [ ] **Step 2: Agregar la excepción a la regla de ruteo por cliente**

Buscar, en la sección "Clients", el párrafo que empieza con:

```
**Client routing rule (from the SRS):** stories whose principal actor is `Administrador`/`Operador` → **web**; `Participante` (incl. `Líder de equipo` acting as participant) → **mobile**, unless a story says otherwise; `Sistema` → **backend**. `Líder de equipo` is **not** a Keycloak role — it is a business attribute (creator of, or transferee of leadership for, a team). Do not implement participant gameplay in web, or admin/operator screens in mobile, unless an SDD explicitly says so.
```

Agregar, como nueva oración al final de ese párrafo:

```
**Explicit exception (HU-04):** a Participante holding `GestionarPartidas` or `GestionarEquipos` uses the **web** panel for that privilege, not mobile — the privilege is the same permission regardless of base role, and its UI is never duplicated for mobile.
```

- [ ] **Step 3: Commit**

```bash
git add CLAUDE.md
git commit -m "docs: CLAUDE.md refleja que el privilegio siempre vive en la web

Las dos frases que asumian 'el rol de quien tiene el privilegio decide
el cliente' quedaron desactualizadas: un Participante con
GestionarPartidas/GestionarEquipos usa el panel web, no una version
mobile. Se documenta como excepcion explicita a la regla de ruteo por
cliente.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

- [ ] **Step 4: Ledger**

```bash
echo "2026-07-15 | HU-04 | s3-task 5 | CLAUDE.md: doctrina de gobernanza y regla de ruteo actualizadas para privilegio-sin-rol. commit <sha>." >> .git/sdd/progress.md
```

---

### Task 6: Verificación en vivo

**Files:** ninguno (verificación).

⚠️ **Requiere autorización del usuario antes de ejecutarse.** Arranca Docker y usa su Keycloak real.

- [ ] **Step 1: Levantar el stack**

```bash
docker compose -f infra/docker-compose.yml --env-file .env up -d --build
docker compose -f infra/docker-compose.yml ps
```

Expected: 9/9 `Up`/`healthy`.

- [ ] **Step 2: Darle `GestionarPartidas` al rol Participante**

Loguearse en la web como `admin`/`admin`, entrar a Gobernanza, marcar «Gestionar partidas» para el
rol Participante y guardar.

- [ ] **Step 3: Entrar como participante y confirmar el área Partidas**

Cerrar sesión, entrar como `participante`/`participante`. **Esperado:** aparece el área Partidas
con «Partidas» y «Nueva partida». Entrar a «Nueva partida»: el panel de creación carga igual que
para un Operador.

- [ ] **Step 4: Confirmar que Identidad sigue sin aparecer**

En la misma sesión de participante. **Esperado:** no aparece el área Identidad, ni por el nav ni
navegando directo a `/identidad/usuarios` (redirige a landing).

- [ ] **Step 5: La simétrica de Equipos**

En Gobernanza, marcar «Gestionar equipos» para Participante y quitar «Gestionar partidas» (si se
quiere probar aislado). Volver a entrar como participante. **Esperado:** aparece el área Equipos
con sus tres items; «Creación de equipos» carga.

- [ ] **Step 6: Mobile sigue igual**

Un participante entra al móvil (con o sin los privilegios recién asignados) y confirma que el panel
de juego es exactamente el de siempre — sin ningún rastro de gobernanza.

- [ ] **Step 7: Restaurar los defaults reales**

```bash
docker exec umbral-postgres psql -U umbral -d umbral_identity -c "DELETE FROM permisos_rol WHERE rol = 3;"
docker compose -f infra/docker-compose.yml --env-file .env restart identity-service
```

Confirmar que el reconciliador vuelve a loguear `Permisos de Participante reconciliados en
Keycloak: []`.

- [ ] **Step 8: Ledger**

```bash
echo "2026-07-15 | HU-04 | s3-task 6 | verificacion en vivo: Participante con GestionarPartidas/GestionarEquipos usa el panel web, Identidad sigue exclusiva de Administrador, mobile intacto. Entorno restaurado a los defaults reales. Sub-proyecto 3/3 COMPLETO." >> .git/sdd/progress.md
```

---

## Notas para quien implemente

- **El AND de rol ya no existe en ningún lado tras la Task 4.** Si un test o un endpoint todavía
  espera que un rol base sea necesario además del privilegio, es una regresión — repórtalo, no lo
  arregles a mano sin avisar.
- **Identidad no se toca.** Ningún cambio de este plan afecta `identidad/usuarios`,
  `identidad/usuarios/nuevo`, `identidad/gobernanza`, ni las rutas `/identity/users` /
  `/identity/governance` del gateway, ni la policy `Administrador` de Identity.
- **Mobile no se toca.** Ningún archivo bajo `mobile/` cambia en ninguna task de este plan.
- **La Task 6 requiere autorización explícita** — no arrancar Docker antes de que el usuario lo
  confirme.
