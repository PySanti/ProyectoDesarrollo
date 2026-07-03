# SP-3g — Alineación de ruteo Operaciones ↔ gateway Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Hacer que todos los endpoints de Operaciones de Sesión y el hub SignalR sean alcanzables a través del gateway YARP, alineando el servicio a la convención de Partidas (hostear bajo el prefijo de nombre; el gateway reenvía el path completo sin transform).

**Architecture:** `SesionesController` pasa de `[Route("")]` a `[Route("operaciones-sesion")]` y el hub se mapea en `operaciones-sesion/hubs/sesion`; el gateway no cambia. Los ContractTests e2e se actualizan al path público prefijado (centralizado en una const `Rutas.Base`). Un test de `negotiate` prueba a nivel-servicio que el hub responde bajo el prefijo.

**Tech Stack:** .NET 8, ASP.NET Core (controllers + SignalR), YARP (sin cambios), xUnit, `WebApplicationFactory`/`OperacionesSesionWebFactory`, fakes a mano (sin Moq).

## Global Constraints

- **Convención:** el servicio hostea sus endpoints de dominio bajo `operaciones-sesion/...` (igual que Partidas bajo `partidas/...`). El gateway sigue siendo reenvío-puro (sin `PathRemovePrefix`).
- **`/health` NO se prefija:** `HealthController` queda `[Route("health")]` (liveness service-local, no proxeado). Los tests que golpean `/health` no cambian.
- **Gateway sin cambios:** ni `appsettings.json` ni `KeycloakJwtExtensions.cs` se tocan (el guard del gateway ya usa `/operaciones-sesion/hubs`).
- **No Moq:** dobles de prueba a mano. Vocabulario de dominio en español.
- **Carve-out (vigente):** dejar SIEMPRE sin commitear `docs/04-sdd/traceability-matrix.md`, `docs/superpowers/specs/2026-06-27-sp3c-runtime-trivia-design.md`, `docs/04-sdd/auditorias/`. Nunca `git add -A` / `git add .` / `git add docs/`. Stagear SOLO los archivos exactos nombrados en cada commit. Prohibido `git checkout/restore/clean/stash/reset` amplios.
- **Mensaje de commit:** termina con exactamente `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>` (sin línea Claude-Session).
- **Suite verde** (UnitTests + ContractTests + IntegrationTests de Operaciones, + IntegrationTests del gateway) al final de cada tarea.

## File Structure

**Producción (modificar):**
- `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Controllers/SesionesController.cs` — `[Route("")]` → `[Route("operaciones-sesion")]`.
- `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Program.cs` — `MapHub` + guard JWT `OnMessageReceived` prefijados.

**Tests (crear/modificar):**
- `services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Rutas.cs` — **crear** (const base compartida).
- `.../ContractTests/SesionEndpointsTests.cs`, `TriviaRuntimeEndpointsTests.cs`, `BdtRuntimeEndpointsTests.cs`, `ReconexionEndpointsTests.cs` — **modificar** (prefijar paths).
- `.../ContractTests/HubNegotiateContractTests.cs` — **crear** (negotiate bajo el prefijo).

**Docs (modificar):**
- `contracts/http/operaciones-sesion-api.md` — quitar el caveat obsoleto de prefijo.

**Carve-out (modificar, NO commitear):**
- `docs/04-sdd/traceability-matrix.md` — fila SP-3g.

---

### Task 1: Prefijar controller + actualizar paths e2e

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Controllers/SesionesController.cs:12`
- Create: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Rutas.cs`
- Modify: `.../ContractTests/SesionEndpointsTests.cs`, `TriviaRuntimeEndpointsTests.cs`, `BdtRuntimeEndpointsTests.cs`, `ReconexionEndpointsTests.cs`

**Interfaces:**
- Consumes: `OperacionesSesionWebFactory.CreateClient()` / `CreateClientAs(Guid)` (existentes).
- Produces: `Rutas.Base` (`"/operaciones-sesion"`) usado por los tests e2e; el servicio ahora responde en `operaciones-sesion/...`.

- [ ] **Step 1: Cambiar la ruta del controller (rompe los 4 archivos e2e — se arreglan en esta misma tarea).**

En `SesionesController.cs` línea 12:
```csharp
[Route("operaciones-sesion")]
```
(era `[Route("")]`). Las acciones son relativas (`partidas/{partidaId:guid}/...`, `mi-sesion`, ...) y no cambian.

- [ ] **Step 2: Correr los ContractTests para verlos fallar (404 por el cambio de ruta).**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj" --filter "FullyQualifiedName~SesionEndpointsTests"`
Expected: FAIL — respuestas 404/aserciones rotas (el servicio ya no responde en `/partidas/...`, ahora en `/operaciones-sesion/partidas/...`).

- [ ] **Step 3: Crear la const base compartida.**

`services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Rutas.cs`:
```csharp
namespace Umbral.OperacionesSesion.ContractTests;

internal static class Rutas
{
    public const string Base = "/operaciones-sesion";
}
```

- [ ] **Step 4: Prefijar TODOS los literales de path de endpoints de dominio en los 4 archivos e2e.**

Regla determinista (aplicar en `SesionEndpointsTests.cs`, `TriviaRuntimeEndpointsTests.cs`, `BdtRuntimeEndpointsTests.cs`, `ReconexionEndpointsTests.cs`):
- Todo literal que empiece por `/partidas/` → anteponer `Rutas.Base`.
- El literal `"/mi-sesion"` → `$"{Rutas.Base}/mi-sesion"`.
- **NO tocar `"/health"`** (no aparece en estos 4 archivos, pero por si acaso: se deja igual).

Ejemplos concretos (patrón interpolado):
```csharp
// antes
await client.PostAsync($"/partidas/{partidaId}/publicacion", ...);
await client.PostAsync($"/partidas/{partidaId}/inscripciones", ...);
await client.DeleteAsync($"/partidas/{partidaId}/inscripciones/mia");
await client.GetAsync($"/partidas/{Guid.NewGuid()}/estado");
await client.PostAsync($"/partidas/{partidaId}/pregunta-actual/avance", ...);
await client.PostAsync($"/partidas/{partidaId}/etapa-actual/avance", ...);
// después
await client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", ...);
await client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inscripciones", ...);
await client.DeleteAsync($"{Rutas.Base}/partidas/{partidaId}/inscripciones/mia");
await client.GetAsync($"{Rutas.Base}/partidas/{Guid.NewGuid()}/estado");
await client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/pregunta-actual/avance", ...);
await client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/etapa-actual/avance", ...);
```
Ejemplo concreto (literal simple, en `ReconexionEndpointsTests.cs`):
```csharp
// antes
var response = await client.GetAsync("/mi-sesion");
// después
var response = await client.GetAsync($"{Rutas.Base}/mi-sesion");
```

> Nota: un literal ya empieza con `/` (p. ej. `"/partidas/..."`); al anteponer `{Rutas.Base}` (que ya trae `/` inicial y sin `/` final) el resultado es `/operaciones-sesion/partidas/...` — correcto. No dupliques barras.

- [ ] **Step 5: Verificar que no quedó ningún path sin prefijar (chequeo de completitud determinista).**

Run:
```bash
grep -rnoE '"(/)?partidas/|"/mi-sesion"' \
  services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/SesionEndpointsTests.cs \
  services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/TriviaRuntimeEndpointsTests.cs \
  services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/BdtRuntimeEndpointsTests.cs \
  services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/ReconexionEndpointsTests.cs
```
Expected: **cero coincidencias** (todo literal de dominio pasa ahora por `{Rutas.Base}/partidas/...` o `{Rutas.Base}/mi-sesion`). Si aparece alguna, prefijarla y repetir.

- [ ] **Step 6: Correr la suite completa de ContractTests + verde.**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj"`
Expected: TODO verde (incluye Health/Realtime/Wiring sin cambios). Cualquier 404 restante = un path olvidado → arreglar.

- [ ] **Step 7: Confirmar que UnitTests siguen verdes (los controller unit tests llaman acciones directamente, no por HTTP — no deberían romperse).**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj"`
Expected: TODO verde.

- [ ] **Step 8: Commit.**

```bash
git add \
  services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Controllers/SesionesController.cs \
  services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Rutas.cs \
  services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/SesionEndpointsTests.cs \
  services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/TriviaRuntimeEndpointsTests.cs \
  services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/BdtRuntimeEndpointsTests.cs \
  services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/ReconexionEndpointsTests.cs
git commit -m "SP-3g T1: SesionesController bajo prefijo operaciones-sesion + paths e2e prefijados (Rutas.Base)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: Prefijar hub + guard JWT del servicio (+ test negotiate)

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Program.cs:94` y `:116`
- Create: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/HubNegotiateContractTests.cs`

**Interfaces:**
- Consumes: `Rutas.Base` (T1); `OperacionesSesionWebFactory.CreateClientAs(Guid)` (autentica vía `X-Test-Sub` + `TestAuthHandler`).
- Produces: hub reachable en `operaciones-sesion/hubs/sesion`; guard JWT del servicio sobre `/operaciones-sesion/hubs/sesion`.

- [ ] **Step 1: Escribir el test negotiate que falla.**

`HubNegotiateContractTests.cs`:
```csharp
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Umbral.OperacionesSesion.ContractTests;

public class HubNegotiateContractTests : IClassFixture<OperacionesSesionWebFactory>
{
    private readonly OperacionesSesionWebFactory _factory;
    public HubNegotiateContractTests(OperacionesSesionWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Negotiate_del_hub_responde_bajo_el_prefijo_operaciones_sesion()
    {
        var client = _factory.CreateClientAs(Guid.NewGuid()); // autenticado (hub lleva [Authorize])

        var response = await client.PostAsync($"{Rutas.Base}/hubs/sesion/negotiate?negotiateVersion=1",
            new StringContent(string.Empty));

        // El hub mapeado bajo el prefijo → negotiate 200 con connectionId.
        // Sin el prefijo (hub en /hubs/sesion) esta ruta daría 404.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("connectionId", body);
    }
}
```
> Si `CreateClientAs` no basta para pasar `[Authorize]` en negotiate (revisar `TestAuthHandler`), usar el mismo mecanismo de autenticación que emplean los tests e2e autenticados de este proyecto. Si negotiate devuelve un 200 sin `connectionId` en alguna versión, relajar a `Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode)` (el discriminador clave es "no 404"). No hardcodear.

- [ ] **Step 2: Correr el test para verlo fallar.**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj" --filter "FullyQualifiedName~HubNegotiateContractTests"`
Expected: FAIL — 404 (el hub aún está mapeado en `/hubs/sesion`, no bajo el prefijo).

- [ ] **Step 3: Prefijar el mapeo del hub.**

`Program.cs` línea 116:
```csharp
app.MapHub<Umbral.OperacionesSesion.Api.Realtime.SesionHub>("operaciones-sesion/hubs/sesion");
```
(era `"hubs/sesion"`).

- [ ] **Step 4: Prefijar el guard JWT del servicio (handshake WS).**

`Program.cs` línea 94:
```csharp
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/operaciones-sesion/hubs/sesion"))
```
(era `StartsWithSegments("/hubs/sesion")`). Con el path completo reenviado por YARP, el servicio ve `/operaciones-sesion/hubs/sesion`.

- [ ] **Step 5: Correr el test negotiate + Realtime + verde.**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj" --filter "FullyQualifiedName~HubNegotiateContractTests|FullyQualifiedName~RealtimeContractTests|FullyQualifiedName~RealtimeWiringTests"`
Expected: PASS (negotiate 200; Realtime doc↔constantes intacto; Wiring composite intacto).

- [ ] **Step 6: Confirmar el gateway (su test del hub sigue verde).**

Run: `dotnet test "gateway/tests/Umbral.Gateway.IntegrationTests/Umbral.Gateway.IntegrationTests.csproj"`
Expected: TODO verde (`Hub_de_operaciones_requiere_autenticacion` sigue asertando 401 anon en `/operaciones-sesion/hubs/sesion`; el gateway no se tocó).

- [ ] **Step 7: Commit.**

```bash
git add \
  services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Program.cs \
  services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/HubNegotiateContractTests.cs
git commit -m "SP-3g T2: hub + guard JWT del servicio bajo prefijo operaciones-sesion + test negotiate

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: Quitar caveat obsoleto del contrato + traceability

**Files:**
- Modify: `contracts/http/operaciones-sesion-api.md` (sección Realtime, párrafo "Notas")
- Modify (CARVE-OUT, NO commitear): `docs/04-sdd/traceability-matrix.md`

**Interfaces:**
- Consumes: —
- Produces: contrato sin la deuda de prefijo (ya resuelta).

- [ ] **Step 1: Reemplazar el caveat obsoleto por una nota positiva.**

En `contracts/http/operaciones-sesion-api.md`, en el párrafo "Notas:" de la sección Realtime, quitar exactamente la última oración (obsoleta):

> `La ruta gateway `/operaciones-sesion/hubs/sesion` está pendiente de resolver una deuda pre-existente de prefijo: el servicio mapea el hub en `/hubs/sesion` y YARP reenvía el path completo sin `PathRemovePrefix`, por lo que la conexión WebSocket a través del gateway queda bloqueada hasta que se aplique ese fix (deuda compartida con todos los endpoints de Operaciones; se resolverá antes del follow-up de cableado de clientes).`

y reemplazarla por:

> `La ruta gateway `/operaciones-sesion/hubs/sesion` es alcanzable end-to-end (SP-3g): el servicio mapea el hub y todos sus endpoints bajo el prefijo `operaciones-sesion`, y YARP reenvía el path completo sin `PathRemovePrefix` — consistente con el resto de los servicios.`

> No tocar la línea del Hub (`Hub: `GET /operaciones-sesion/hubs/sesion` ...`) ni la mención de `access_token`: `RealtimeContractTests` asevera que ambas siguen presentes.

- [ ] **Step 2: Confirmar que RealtimeContractTests sigue verde (el doc mantiene la URL del hub y `access_token`).**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj" --filter "FullyQualifiedName~RealtimeContractTests"`
Expected: PASS (11 casos: nombres de mensaje + URL del hub + `access_token`).

- [ ] **Step 3: Escribir la fila de traceability (NO commitear).**

Añadir a `docs/04-sdd/traceability-matrix.md`, como nueva fila tras la fila SP-3f-2 (7 columnas, mismo formato que las vecinas):
```markdown
| Alineación de ruteo Operaciones ↔ gateway (SP-3g) | Operaciones adopta la convención de Partidas: SesionesController `[Route("operaciones-sesion")]` + hub `operaciones-sesion/hubs/sesion` + guard JWT del servicio prefijado; gateway sin cambios (reenvío-puro, sin PathRemovePrefix). Resuelve el Important del review de SP-3f-2 (hub/endpoints inalcanzables vía gateway). Tests e2e a nivel-servicio en el path público prefijado (Rutas.Base) + test negotiate del hub bajo el prefijo | Operaciones de Sesión | Gateway (reenvío-puro, sin cambios) | docs/superpowers/specs/2026-06-30-sp3g-alineacion-ruteo-gateway-design.md · docs/superpowers/plans/2026-06-30-sp3g-alineacion-ruteo-gateway.md | contracts/http/operaciones-sesion-api.md | Implemented — suite verde. **Gap documentado:** e2e real gateway→servicio-vivo no testeable en el harness (destinos no corren en test); cubierto por contract tests a nivel-servicio en el path público. **Forward-looking:** identity/puntuaciones deben usar `[Route("<servicio>")]` al ganar controllers de dominio. |
```

- [ ] **Step 4: Correr la suite completa de Operaciones + verde.**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj"` · `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj"` · `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/Umbral.OperacionesSesion.IntegrationTests.csproj"`
Expected: TODO verde.

- [ ] **Step 5: Commit (SOLO el contrato; la traceability queda sin commitear).**

```bash
git add contracts/http/operaciones-sesion-api.md
git commit -m "SP-3g T3: contrato — la ruta del hub vía gateway ya es alcanzable (quita caveat de prefijo)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

- [ ] **Step 6: Verificar el carve-out post-commit.**

Run: `git status --short`
Expected: siguen sin commitear SOLO `docs/04-sdd/traceability-matrix.md` (M), `docs/superpowers/specs/2026-06-27-sp3c-runtime-trivia-design.md` (M), `docs/04-sdd/auditorias/` (??). Nada más staged.

---

## Self-Review

**1. Spec coverage:**
- SesionesController prefijado → T1. ✓
- Hub + guard JWT servicio prefijados → T2. ✓
- Gateway sin cambios → constraints + T2 Step 6 (verifica que sigue verde). ✓
- ContractTests al path público prefijado (const base) → T1. ✓
- Prueba a nivel-servicio de reachability del hub (negotiate) → T2. ✓
- Health sin prefijar → constraints. ✓
- Quitar caveat obsoleto del contrato → T3. ✓
- Traceability (carve-out) → T3. ✓
- Gap documentado (proxy-vivo no testeable) → traceability T3 + spec. ✓
- Forward-looking identity/puntuaciones → traceability T3. ✓

**2. Placeholder scan:** sin TBD/TODO; cada paso con código/comando real. Las notas de fallback (negotiate 200 vs no-404; auth de negotiate) apuntan a verificación de comportamiento real del framework, no son huecos.

**3. Type consistency:** `Rutas.Base` (`"/operaciones-sesion"`) definido en T1 y consumido en T1/T2; `[Route("operaciones-sesion")]` (T1) coincide con `MapHub("operaciones-sesion/hubs/sesion")` y `StartsWithSegments("/operaciones-sesion/hubs/sesion")` (T2) y con el path del test negotiate `{Rutas.Base}/hubs/sesion/negotiate`. Consistente.

## Execution Handoff

Plan completo y guardado en `docs/superpowers/plans/2026-06-30-sp3g-alineacion-ruteo-gateway.md`. Dos opciones de ejecución:

1. **Subagent-Driven (recomendada)** — subagente fresco por tarea + review dos etapas, commit + ledger por tarea.
2. **Inline (executing-plans)** — lotes con checkpoints en esta sesión.

¿Cuál?
