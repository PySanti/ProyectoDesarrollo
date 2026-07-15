# Bloque 7a — Regresión gateway equipos-admin — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** La pantalla de administración de equipos (web) vuelve a pasar por el gateway y la ruta `/identity/admin/teams` queda protegida con RBAC gruesa `Administrador` en YARP. Cierra RNF-21, RNF-22 y HU-09 (paquete R2 del informe de completitud 2026-07-11).

**Architecture:** Dos cambios independientes y pequeños: (1) el gateway gana una ruta explícita `identity-admin-teams` con policy `Administrador` (hoy ese path cae en el catch-all `identity` con policy `Default` = cualquier autenticado); (2) `frontend/src/api/adminTeamsApi.ts` cambia su base URL de `VITE_IDENTITY_API_BASE_URL` (var retirada, llamada directa al servicio) a `VITE_GATEWAY_BASE_URL` (mismo patrón que `identityApi.ts`). Los paths relativos (`/identity/admin/teams/...`) ya son correctos — solo cambia la base.

**Tech Stack:** YARP (gateway .NET 8, config en `appsettings.json`), xUnit + `WebApplicationFactory` (tests gateway), React/Vite/TypeScript + vitest (frontend).

## Global Constraints

- Rama de trabajo: `feature/bloque-7` (ya activa). NO crear ramas nuevas.
- Commits terminan con: `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`
- PROHIBIDO a implementadores: `git stash/reset/checkout/restore/clean`. Solo `git add <paths exactos>` + `git commit`.
- Gates frontend: `npx vitest run` + `npx tsc -b` + `npm run build` desde `frontend/`. `tsc -b`/build generan artefactos (`tsconfig*.tsbuildinfo`, `vite.config.js/.d.ts`, `vitest.config.js/.d.ts`) — **borrarlos, nunca commitearlos**.
- Gate gateway: `dotnet test gateway/Umbral.Gateway.sln` (con `export DOTNET_ROOT=/snap/dotnet-sdk/current` si dotnet falla al resolver SDK).
- No añadir dependencias nuevas.
- Textos de UI y dominio en español; código/identificadores siguen el estilo existente del archivo tocado.

---

### Task 1: Ruta `identity-admin-teams` en el gateway + contrato

**Files:**
- Modify: `gateway/src/Umbral.Gateway/appsettings.json` (bloque `ReverseProxy.Routes`, tras `identity-users`)
- Modify: `gateway/tests/Umbral.Gateway.IntegrationTests/GatewayEndpointsTests.cs` (añadir 4 tests al final de la clase, antes del cierre)
- Modify: `contracts/http/gateway-api.md` (tabla de rutas: fila nueva)

**Interfaces:**
- Consumes: helpers existentes del test file: `CreateClientWithRoles(string roles)` y `AssertPolicyPassed(HttpResponseMessage)` (ya definidos en `GatewayEndpointsTests.cs:88-109`).
- Produces: ruta YARP `identity-admin-teams` (Order 1, policy `Administrador`) que las llamadas del frontend (Task 2) atraviesan.

- [ ] **Step 1: Escribir los 4 tests que fallan**

Añadir al final de `GatewayEndpointsTests.cs` (dentro de la clase, después del último `[Fact]` existente):

```csharp
    [Fact]
    public async Task IdentityAdminTeams_sin_token_es_401()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/identity/admin/teams");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task IdentityAdminTeams_con_Participante_es_403()
    {
        // Sin la ruta explícita, /identity/admin/teams caía en el catch-all identity (policy
        // Default = cualquier autenticado); este 403 pinnea la RBAC gruesa Administrador-only.
        var client = CreateClientWithRoles("Participante");
        var response = await client.GetAsync("/identity/admin/teams");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task IdentityAdminTeams_con_Operador_es_403()
    {
        var client = CreateClientWithRoles("Operador");
        var response = await client.GetAsync("/identity/admin/teams");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task IdentityAdminTeams_con_Administrador_pasa_la_politica()
    {
        var client = CreateClientWithRoles("Administrador");
        var response = await client.GetAsync("/identity/admin/teams/cualquier-cosa");
        AssertPolicyPassed(response);
    }
```

- [ ] **Step 2: Correr y verificar que fallan los 2 de rol**

Run: `export DOTNET_ROOT=/snap/dotnet-sdk/current; dotnet test gateway/Umbral.Gateway.sln --filter "IdentityAdminTeams"`
Expected: `IdentityAdminTeams_con_Participante_es_403` y `IdentityAdminTeams_con_Operador_es_403` FALLAN (hoy pasan la política Default y devuelven 502/504, no 403). Los otros 2 pasan ya (401 por fallback, Administrador pasa) — está bien: pinnean el comportamiento que no debe romperse.

- [ ] **Step 3: Añadir la ruta en `appsettings.json`**

En `gateway/src/Umbral.Gateway/appsettings.json`, insertar **después** del bloque `"identity-users"` (línea ~23) y **antes** de `"identity-teams-listing"`:

```json
      "identity-admin-teams": {
        "ClusterId": "identity",
        "Order": 1,
        "Match": { "Path": "/identity/admin/teams/{**catch-all}" },
        "AuthorizationPolicy": "Administrador"
      },
```

Nota: `{**catch-all}` matchea también el path exacto `/identity/admin/teams` (catch-all vacío) — mismo patrón que `identity-users`. Order 1 gana al catch-all `identity` (Order 2); no colisiona con `identity-teams` (`/identity/teams/...` es otro segmento).

- [ ] **Step 4: Correr los tests y verificar que pasan los 4**

Run: `export DOTNET_ROOT=/snap/dotnet-sdk/current; dotnet test gateway/Umbral.Gateway.sln --filter "IdentityAdminTeams"`
Expected: 4/4 PASS.

- [ ] **Step 5: Correr la suite completa del gateway (regresión)**

Run: `export DOTNET_ROOT=/snap/dotnet-sdk/current; dotnet test gateway/Umbral.Gateway.sln`
Expected: todo verde (24 tests previos + 4 nuevos = 28).

- [ ] **Step 6: Fila en el contrato**

En `contracts/http/gateway-api.md`, en la tabla de rutas (la que tiene columnas Ruta/Order/Policy/Cluster — misma tabla donde está `identity-users`), añadir tras la fila `identity-users`:

```markdown
| `identity-admin-teams` | `/identity/admin/teams/{**catch-all}` | 1 | `Administrador` | identity | CRUD administrativo de equipos (HU-09, Bloque 4A); RBAC gruesa añadida en Bloque 7a |
```

Ajustar el orden/número de columnas al formato EXACTO de la tabla existente (leerla antes de editar; si la tabla no tiene columna de notas, poner la nota como texto bajo la tabla siguiendo el estilo del archivo).

- [ ] **Step 7: Commit**

```bash
git add gateway/src/Umbral.Gateway/appsettings.json gateway/tests/Umbral.Gateway.IntegrationTests/GatewayEndpointsTests.cs contracts/http/gateway-api.md
git commit -m "feat(gateway): ruta identity-admin-teams con RBAC Administrador (7a)

/identity/admin/teams caía en el catch-all identity (policy Default);
ahora exige rol Administrador a nivel de ruta, como identity-users.

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 2: `adminTeamsApi.ts` vía gateway

**Files:**
- Modify: `frontend/src/api/adminTeamsApi.ts:41-49` (resolución de base URL)
- Modify: `frontend/src/api/adminTeamsApi.test.ts` (todas las ocurrencias de la var de entorno)

**Interfaces:**
- Consumes: ruta del gateway creada en Task 1 (`/identity/admin/teams` con policy Administrador).
- Produces: módulo `adminTeamsApi` (funciones `listAdminTeams`, `getAdminTeam`, `createAdminTeam`, `renameAdminTeam`, `reassignAdminTeamLeader`, `setAdminTeamEstado`, `deleteAdminTeam` — firmas SIN cambio) que `TeamsAdminPage.tsx` ya consume; ahora resuelve base desde `VITE_GATEWAY_BASE_URL`.

- [ ] **Step 1: Actualizar los tests para que esperen la var del gateway**

En `frontend/src/api/adminTeamsApi.test.ts`, reemplazar TODAS las ocurrencias (hay 8+, líneas 10, 40, 66, 98, 131, 170, 206, 230…):

```
vi.stubEnv("VITE_IDENTITY_API_BASE_URL", ...)  →  vi.stubEnv("VITE_GATEWAY_BASE_URL", ...)
```

(los valores stubbeados `https://gw.example.test` se conservan tal cual). Si el archivo tiene un test de "missing env" que espera el mensaje `Missing VITE_IDENTITY_API_BASE_URL environment variable.`, actualizar el texto esperado a `Missing VITE_GATEWAY_BASE_URL environment variable.`; si NO existe ese test, añadirlo siguiendo el patrón del archivo.

- [ ] **Step 2: Correr y verificar que fallan**

Run: `cd frontend && npx vitest run src/api/adminTeamsApi.test.ts`
Expected: FAIL — el módulo sigue leyendo `VITE_IDENTITY_API_BASE_URL` (ahora sin stub) y lanza `Missing VITE_IDENTITY_API_BASE_URL environment variable.`

- [ ] **Step 3: Cambiar la resolución de base URL en el módulo**

En `frontend/src/api/adminTeamsApi.ts`, reemplazar las líneas 41-49:

```typescript
const baseUrl = import.meta.env.VITE_GATEWAY_BASE_URL as string | undefined;

function resolveBaseUrl(): string {
  if (!baseUrl) {
    throw new Error("Missing VITE_GATEWAY_BASE_URL environment variable.");
  }

  return baseUrl.replace(/\/$/, "");
}
```

(Idéntico al patrón de `identityApi.ts:46-54`. Nada más cambia: los paths `/identity/admin/teams...` ya son los del gateway.)

- [ ] **Step 4: Correr los tests del módulo**

Run: `cd frontend && npx vitest run src/api/adminTeamsApi.test.ts`
Expected: PASS completo.

- [ ] **Step 5: Verificar que no queda ninguna referencia a la var retirada**

Run: `grep -rn "VITE_IDENTITY_API_BASE_URL" frontend/src/`
Expected: **cero resultados**.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/api/adminTeamsApi.ts frontend/src/api/adminTeamsApi.test.ts
git commit -m "fix(web): adminTeamsApi via gateway (VITE_GATEWAY_BASE_URL) (7a)

Regresión de Bloque 4A: llamaba directo a identity con la var retirada
VITE_IDENTITY_API_BASE_URL (rompía con .env regenerado y por CORS).
Paths relativos sin cambio; mismo patrón que identityApi.

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 3: Gates completos del slice

**Files:**
- Ninguno nuevo (solo verificación; si un gate falla, arreglar y commitear el fix).

**Interfaces:**
- Consumes: Tasks 1 y 2 completas.
- Produces: evidencia de cierre del slice 7a para el ledger.

- [ ] **Step 1: Suite frontend completa**

Run: `cd frontend && npx vitest run`
Expected: todo verde (211+ tests).

- [ ] **Step 2: Typecheck + build frontend**

Run: `cd frontend && npx tsc -b && npm run build`
Expected: sin errores. **Después borrar artefactos generados** (`tsconfig*.tsbuildinfo`, `vite.config.js`, `vite.config.d.ts`, `vitest.config.js`, `vitest.config.d.ts` si aparecen): `git status` debe quedar limpio de archivos nuevos sin trackear de ese tipo.

- [ ] **Step 3: Suite gateway completa (re-confirmación)**

Run: `export DOTNET_ROOT=/snap/dotnet-sdk/current; dotnet test gateway/Umbral.Gateway.sln`
Expected: 28/28 verde.

- [ ] **Step 4: Verificación manual del criterio de aceptación**

Run: `grep -n "VITE_GATEWAY_BASE_URL" frontend/.env.example && grep -rn "VITE_IDENTITY_API_BASE_URL" frontend/ --include="*.ts" --include="*.tsx" --include="*.example"`
Expected: la primera línea muestra la var en `.env.example` (ya estaba); la segunda: cero resultados. Con esto, TeamsAdminPage funciona con `.env` regenerado desde `.env.example`.

- [ ] **Step 5: Anotar cierre en el ledger**

Añadir a `.git/sdd/progress.md` (append, no editar lo previo): línea con "7a DONE" + hashes de los 2 commits + evidencia de gates (números exactos de tests).
