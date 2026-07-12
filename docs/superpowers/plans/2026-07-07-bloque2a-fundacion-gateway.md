# Bloque 2a — Fundación gateway en clientes: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Todo el tráfico identity/equipos de web y mobile pasa por el gateway YARP (`:5080`); CORS en el borde correcto; pantallas viejas de trivia/bdt intactas.

**Architecture:** Swap puro de base URL en clientes (el gateway pasa el path sin transforms y los servicios hospedan bajo su prefijo). Único cambio backend: CORS en gateway + retiro del CORS de identity (evita `Access-Control-Allow-Origin` duplicado al proxyar).

**Tech Stack:** ASP.NET Core 8 (gateway YARP), React/Vite (web), React Native/Expo (mobile), xunit + WebApplicationFactory, vitest, node --test.

**Spec:** `docs/superpowers/specs/2026-07-07-bloque2a-fundacion-gateway-design.md`

## Global Constraints

- Rama: `feature/bloque-2`. Commits con trailer `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- PROHIBIDO a subagentes: `git stash/reset/checkout/restore/clean`. Stage SOLO los archivos exactos listados, uno por uno.
- Cero cambios en contratos (`contracts/`), dominio, ni HUs.
- `triviaApi.ts`/`bdtApi.ts` (web) y `features/bdt|trivia` (mobile) NO se tocan.
- Nombres de env vars exactos: `VITE_GATEWAY_BASE_URL`, `EXPO_PUBLIC_GATEWAY_BASE_URL`, `GATEWAY_PORT`, `CORS_ALLOWED_ORIGINS`.

---

### Task 1: CORS en el gateway + retiro del CORS de identity

**Files:**
- Modify: `gateway/src/Umbral.Gateway/Program.cs`
- Modify: `gateway/tests/Umbral.Gateway.IntegrationTests/GatewayEndpointsTests.cs`
- Modify: `gateway/.env.example`
- Modify: `infra/docker-compose.yml` (solo env del servicio `gateway`)
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Program.cs`

**Interfaces:**
- Produces: gateway acepta CORS de orígenes en `CORS_ALLOWED_ORIGINS` (CSV, default `http://localhost:5173`), con credentials. Identity deja de emitir headers CORS.

- [ ] **Step 1: Tests que fallan (gateway).** Agregar al final de la clase `GatewayEndpointsTests` (antes del cierre `}` de la clase, `GatewayEndpointsTests.cs:206`):

```csharp
    [Fact]
    public async Task Preflight_cors_desde_origen_permitido_pasa()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Options, "/identity/users");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(request);

        // El middleware CORS responde el preflight ANTES de la política de autorización.
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("http://localhost:5173",
            Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task Respuesta_con_origin_lleva_un_solo_allow_origin()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "http://localhost:5173");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("http://localhost:5173",
            Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }
```

- [ ] **Step 2: Verificar que fallan.**

Run: `dotnet test gateway/Umbral.Gateway.sln --filter "Preflight_cors_desde_origen_permitido_pasa|Respuesta_con_origin_lleva_un_solo_allow_origin"`
Expected: FAIL — preflight devuelve 401 (fallback policy, sin middleware CORS) y `/health` no trae header `Access-Control-Allow-Origin`.
Nota: si el `.sln` no está en esa ruta, localizarlo con `ls gateway/*.sln`.

- [ ] **Step 3: Implementar CORS en `gateway/src/Umbral.Gateway/Program.cs`.** Tras el bloque de AuthZ (después de la línea `.SetFallbackPolicy(...)`, línea 19) insertar:

```csharp
// CORS del borde: el navegador (web :5173) llama al gateway; los orígenes vienen de env.
// AllowCredentials: lo requerirá la negociación SignalR de los slices 2c/2f.
var corsOrigins = (builder.Configuration["CORS_ALLOWED_ORIGINS"] ?? "http://localhost:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));
```

Y en el pipeline, inmediatamente ANTES de `app.UseAuthentication();` (línea 23):

```csharp
app.UseCors();
```

- [ ] **Step 4: Verificar que pasan.**

Run: `dotnet test gateway/Umbral.Gateway.sln`
Expected: PASS toda la suite (las 2 nuevas + las existentes).

- [ ] **Step 5: Retirar CORS de identity.** En `services/identity-service/src/Umbral.IdentityService.Api/Program.cs`:
  - Eliminar el bloque completo `builder.Services.AddCors(...)` (líneas 15-24):

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
```

  - Eliminar la línea `app.UseCors("FrontendDev");` (línea 148).

Razón (dejar como comentario NO — no hace falta comentario; el spec lo documenta): tras 2a el browser no llama a identity directo, y si identity siguiera emitiendo `Access-Control-Allow-Origin`, YARP lo reenviaría y se duplicaría con el del gateway (el browser rechaza duplicados).

- [ ] **Step 6: Suite de identity verde.**

Run: `dotnet test services/identity-service/Umbral.IdentityService.sln`
Expected: PASS completo (ningún test referencia CORS — verificado).
Nota: si el `.sln` no está en esa ruta, localizarlo con `ls services/identity-service/*.sln`.

- [ ] **Step 7: Documentar env.** En `gateway/.env.example`, agregar al final:

```
# Origenes permitidos para CORS (CSV). El navegador del frontend llama al gateway.
CORS_ALLOWED_ORIGINS='http://localhost:5173'
```

En `infra/docker-compose.yml`, en `services.gateway.environment`, agregar (junto a las demás env):

```yaml
      CORS_ALLOWED_ORIGINS: http://localhost:5173
```

- [ ] **Step 8: Commit.**

```bash
git add gateway/src/Umbral.Gateway/Program.cs gateway/tests/Umbral.Gateway.IntegrationTests/GatewayEndpointsTests.cs gateway/.env.example infra/docker-compose.yml services/identity-service/src/Umbral.IdentityService.Api/Program.cs
git commit -m "feat(gateway): CORS en el borde + retiro CORS de identity (bloque 2a)

El browser ahora llama al gateway; identity ya no emite headers CORS
(duplicaria Access-Control-Allow-Origin al proxyar).

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 2: Web vía gateway (`VITE_GATEWAY_BASE_URL`)

**Files:**
- Modify: `frontend/src/api/identityApi.ts:47-55`
- Modify: `frontend/src/api/identityApi.test.ts` (todas las apariciones de la var)
- Modify: `frontend/.env.example:4`
- Modify: `.env.example` (raíz — nueva `GATEWAY_PORT`)

**Interfaces:**
- Produces: web resuelve TODA la base identity desde `VITE_GATEWAY_BASE_URL`. `VITE_IDENTITY_API_BASE_URL` deja de existir en el repo.

- [ ] **Step 1: Renombrar la var en código y tests (sed, cubre también el mensaje de error).**

```bash
sed -i 's/VITE_IDENTITY_API_BASE_URL/VITE_GATEWAY_BASE_URL/g' frontend/src/api/identityApi.ts frontend/src/api/identityApi.test.ts
```

El resultado esperado en `frontend/src/api/identityApi.ts:47-55`:

```typescript
const baseUrl = import.meta.env.VITE_GATEWAY_BASE_URL as string | undefined;

function resolveBaseUrl(): string {
  if (!baseUrl) {
    throw new Error("Missing VITE_GATEWAY_BASE_URL environment variable.");
  }

  return baseUrl.replace(/\/$/, "");
}
```

- [ ] **Step 2: Envs.** En `frontend/.env.example` reemplazar la línea 4:

```
VITE_IDENTITY_API_BASE_URL=http://${KEYCLOAK_HOST:-localhost}:${IDENTITY_PORT:-5000}
```

por:

```
VITE_GATEWAY_BASE_URL=http://${KEYCLOAK_HOST:-localhost}:${GATEWAY_PORT:-5080}
```

En `.env.example` (raíz), en la sección `# --- Puertos de los microservicios ---`, agregar tras `IDENTITY_PORT=5000`:

```
GATEWAY_PORT=5080
```

- [ ] **Step 3: Verificar cero referencias residuales.**

Run: `grep -rn "VITE_IDENTITY_API_BASE_URL" frontend/ --include="*.ts" --include="*.tsx" --include="*.example" ; echo "exit=$?"`
Expected: sin matches (`exit=1`).

- [ ] **Step 4: Suite verde + build.**

Run: `cd frontend && npm test && npm run build`
Expected: PASS y build sin errores de tipos.

- [ ] **Step 5: Commit.**

```bash
git add frontend/src/api/identityApi.ts frontend/src/api/identityApi.test.ts frontend/.env.example .env.example
git commit -m "feat(web): trafico identity via gateway con VITE_GATEWAY_BASE_URL (bloque 2a)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 3: Mobile vía gateway (`EXPO_PUBLIC_GATEWAY_BASE_URL`)

**Files:**
- Modify: `mobile/src/config/env.ts:13` (campo `identityApiBaseUrl` → `gatewayApiBaseUrl`)
- Modify: 6 containers en `mobile/src/features/teams/`: `CreateTeamScreenContainer.tsx`, `InvitationsScreenContainer.tsx`, `InviteMemberScreenContainer.tsx`, `LeaveTeamScreenContainer.tsx`, `TransferLeadershipScreenContainer.tsx` (y cualquier otro que aparezca en el grep del Step 1)
- Modify: `mobile/.env.example:14` + eliminar var muerta `EXPO_PUBLIC_TEAM_API_BASE_URL` si aparece
- Modify: `mobile/run-local.sh` (generador del `.env` literal)

**Interfaces:**
- Consumes: los API de teams reciben `apiBaseUrl` por parámetro — NO cambian; solo cambia el valor que los containers les pasan.
- Produces: `mobileEnv.gatewayApiBaseUrl` (reemplaza a `mobileEnv.identityApiBaseUrl` en todo `mobile/`).

- [ ] **Step 1: Renombrar campo y var por sed (código + tests de una vez).**

```bash
grep -rl "identityApiBaseUrl\|EXPO_PUBLIC_IDENTITY_API_BASE_URL" mobile/src mobile/tests | xargs sed -i 's/identityApiBaseUrl/gatewayApiBaseUrl/g; s/EXPO_PUBLIC_IDENTITY_API_BASE_URL/EXPO_PUBLIC_GATEWAY_BASE_URL/g'
```

Resultado esperado en `mobile/src/config/env.ts:13`:

```typescript
  gatewayApiBaseUrl: required(process.env.EXPO_PUBLIC_GATEWAY_BASE_URL, "EXPO_PUBLIC_GATEWAY_BASE_URL"),
```

y en los containers: `apiBaseUrl={mobileEnv.gatewayApiBaseUrl}`.

- [ ] **Step 2: Envs y generador.** En `mobile/.env.example`, reemplazar la línea:

```
EXPO_PUBLIC_IDENTITY_API_BASE_URL=http://${IP}:${IDENTITY_PORT:-5000}
```

por:

```
EXPO_PUBLIC_GATEWAY_BASE_URL=http://${IP}:${GATEWAY_PORT:-5080}
```

En `mobile/run-local.sh`, dentro del heredoc `cat > .env <<EOF`:
  - Reemplazar `EXPO_PUBLIC_IDENTITY_API_BASE_URL=http://${IP}:${IDENTITY_PORT:-5000}` por `EXPO_PUBLIC_GATEWAY_BASE_URL=http://${IP}:${GATEWAY_PORT:-5080}`.
  - Eliminar la línea `EXPO_PUBLIC_TEAM_API_BASE_URL=http://${IP}:${TEAM_PORT:-5099}` (var muerta — cero consumidores en `mobile/src/`, hallazgo de la auditoría). Eliminarla también de `mobile/.env.example` si aparece.

- [ ] **Step 3: Verificar cero referencias residuales.**

Run: `grep -rn "EXPO_PUBLIC_IDENTITY_API_BASE_URL\|identityApiBaseUrl\|EXPO_PUBLIC_TEAM_API_BASE_URL" mobile/ --include="*.ts" --include="*.tsx" --include="*.js" --include="*.sh" --include="*.example" | grep -v node_modules ; echo "exit=$?"`
Expected: sin matches (`exit=1`).

- [ ] **Step 4: Suite + typecheck verdes.**

Run: `cd mobile && npm test && npm run typecheck`
Expected: PASS ambos (Node ≥ 20.19.4).

- [ ] **Step 5: Commit.**

```bash
git add mobile/src/config/env.ts mobile/.env.example mobile/run-local.sh
git add mobile/src/features/teams/CreateTeamScreenContainer.tsx mobile/src/features/teams/InvitationsScreenContainer.tsx mobile/src/features/teams/InviteMemberScreenContainer.tsx mobile/src/features/teams/LeaveTeamScreenContainer.tsx mobile/src/features/teams/TransferLeadershipScreenContainer.tsx
# + cualquier otro archivo tocado por el sed del Step 1 (verificar con git status --short)
git commit -m "feat(mobile): trafico identity/equipos via gateway + retiro var muerta TEAM (bloque 2a)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 4: Verificación E2E viva (controller — NO subagente)

**Files:** ninguno (solo verificación).

- [ ] **Step 1: Levantar mínimo vivo.** Infra por compose (postgres/rabbitmq/keycloak) + identity y gateway por `dotnet run` (o el stack compose completo si ya está construido).
- [ ] **Step 2: Preflight real.**

Run: `curl -s -i -X OPTIONS http://localhost:5080/identity/users -H "Origin: http://localhost:5173" -H "Access-Control-Request-Method: POST" | head -12`
Expected: `204` con `Access-Control-Allow-Origin: http://localhost:5173` (una sola vez).

- [ ] **Step 3: Smoke web.** `frontend/run-local.sh`, login admin (creds de test del realm — `infra/keycloak/README.md`), panel de usuarios carga vía `:5080` (verificar en la pestaña Network que las llamadas van al 5080).
- [ ] **Step 4: Smoke mobile (curl).** Token de participante por password grant contra Keycloak y `curl http://localhost:5080/identity/teams/mine -H "Authorization: Bearer $TOKEN"` → 200/404 de dominio (no 401/403 de gateway).

---

### Task 5: Docs + traceability

**Files:**
- Modify: `GUIA-LEVANTAMIENTO.md` (sección de levantamiento de clientes)
- Modify: `docs/04-sdd/traceability-matrix.md` (fila Bloque 2a)

- [ ] **Step 1: GUIA.** En la sección "## Levantamiento de microservicios", agregar tras el encabezado (antes de "### Linux") el aviso:

```markdown
> ⚠️ **Desde el Bloque 2a, web y mobile consumen identity/equipos A TRAVÉS del gateway** (`GATEWAY_PORT`, default 5080). El gateway es parte obligatoria del levantamiento local: `./gateway/run-local.sh` antes de abrir los clientes. Trivia/BDT viejos siguen directos (:5015/:5016) hasta los slices 2b-2e.
```

Y en la lista de terminales de Linux/Powershell, agregar el gateway como primera terminal:

```markdown
# Terminal 0 (OBLIGATORIA: entrada única del backend para los clientes)
./gateway/run-local.sh
```

- [ ] **Step 2: Traceability.** Agregar fila en la tabla de `docs/04-sdd/traceability-matrix.md` (formato de las filas existentes; ver la fila del Bloque 6 como referencia):
  - Slice: `Bloque 2a — fundación gateway`; Requisitos: `RNF-21 (parcial: identity/equipos)`; Evidencia: spec + rutas de código tocadas + suites verdes.

- [ ] **Step 3: Commit.**

```bash
git add GUIA-LEVANTAMIENTO.md docs/04-sdd/traceability-matrix.md
git commit -m "docs(bloque2a): guia levantamiento via gateway + traceability RNF-21 parcial

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Modelos y reviewers (workflow subagent-driven)

| Task | Implementer | Review |
|---|---|---|
| T1 | sonnet (cross-service, juicio) | sonnet |
| T2 | haiku (mecánico, código verbatim) | sonnet |
| T3 | haiku (mecánico, código verbatim) | sonnet |
| T4 | controller (yo, con stack vivo) | — |
| T5 | haiku | sonnet |

Review final whole-branch: opus (decisión usuario al llegar).
