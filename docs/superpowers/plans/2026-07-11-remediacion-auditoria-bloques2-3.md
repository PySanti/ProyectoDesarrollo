# Remediación Auditoría Bloques 2-3 (R1-R4) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cerrar los 4 hallazgos del informe de conformidad (H1 admin observador en web; H2/H3 drift de contratos; H4 scripts mobile con vars legacy).

**Architecture:** R1 es solo UI web: abrir rutas/nav al Administrador y ocultar acciones mutantes con una prop `puedeOperar: boolean` calculada en `App.tsx` y pasada por props (sin contexto nuevo, sin tocar backend/gateway — ya autorizan). R2-R4 son ediciones puntuales de docs y scripts.

**Tech Stack:** React 18 + Vite + TypeScript, vitest + testing-library (patrones existentes de `App.test.tsx` / `navConfig.test.tsx`).

## Global Constraints

- Spec: `docs/superpowers/specs/2026-07-11-remediacion-auditoria-bloques2-3-design.md` (dadc5ad).
- Rama `feature/bloque-2`. Un commit por tarea, con trailer exacto: `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- PROHIBIDO a los implementers: `git stash/reset/checkout/restore/clean`. Solo `git add <rutas exactas>` + `git commit`.
- No cambiar `label`/`id`/`data-testid`/ARIA existentes; solo condicionar el render de acciones.
- Gates web (T1-T3): `cd frontend && npm test` verde, `npx tsc -b` limpio, `npm run build` OK. Borrar artefactos generados si aparecen (`frontend/tsconfig*.tsbuildinfo`, `vite.config.js`, `vite.config.d.ts`, `vitest.config.js`, `vitest.config.d.ts`) — nunca commitearlos.
- Baseline actual: frontend 182 tests / 29 archivos.

---

### Task 1: Rutas y navegación para Administrador (R1 estructura)

**Files:**
- Modify: `frontend/src/shell/navConfig.tsx`
- Modify: `frontend/src/shell/navConfig.test.tsx`
- Modify: `frontend/src/app/App.tsx` (rutas `partidas`, `partidas/:partidaId`, `partidas/:partidaId/sesion`)
- Modify: `frontend/src/app/App.test.tsx`

**Interfaces:**
- Consumes: `areasForRoles(roles: string[]): NavAreaDef[]` existente; `RequireRole` con `need: string | string[]` existente.
- Produces: `NavItemDef` gana `roles?: readonly Role[]` (opcional; ausente = hereda visibilidad del área); `areasForRoles` devuelve áreas con `items` ya filtrados por rol. Las 3 rutas quedan `need={["Operador", "Administrador"]}`.

- [ ] **Step 1: Tests que fallan — navConfig**

En `frontend/src/shell/navConfig.test.tsx`, dentro de `describe("areasForRoles")`, actualizar los dos tests desactualizados (minor diferido de 3b) y añadir el filtrado de items. El admin ahora VE partidas:

```tsx
it("shows Identidad, Partidas, Puntuaciones and Equipos to an admin", () => {
  expect(areasForRoles(["Administrador"]).map((area) => area.id)).toEqual([
    "identidad",
    "partidas",
    "puntuaciones",
    "equipos"
  ]);
});

it("shows Partidas, Puntuaciones and Equipos, but not Identidad, to an operator", () => {
  expect(areasForRoles(["Operador"]).map((area) => area.id)).toEqual([
    "partidas",
    "puntuaciones",
    "equipos"
  ]);
});

it("hides 'Nueva partida' from an admin but keeps it for an operator", () => {
  const partidasAdmin = areasForRoles(["Administrador"]).find((a) => a.id === "partidas");
  expect(partidasAdmin?.items.map((i) => i.label)).toEqual(["Partidas"]);
  const partidasOperador = areasForRoles(["Operador"]).find((a) => a.id === "partidas");
  expect(partidasOperador?.items.map((i) => i.label)).toEqual(["Partidas", "Nueva partida"]);
});
```

Nota: el test existente "shows Equipos to both..." sigue válido sin cambios.

- [ ] **Step 2: Correr y ver fallar**

Run: `cd frontend && npx vitest run src/shell/navConfig.test.tsx`
Expected: FAIL (admin sin "partidas"; items sin filtrar).

- [ ] **Step 3: Implementar navConfig**

En `frontend/src/shell/navConfig.tsx`:

```tsx
export interface NavItemDef {
  label: string;
  path: string;
  icon: IconComponent;
  /** Sin `roles`: el item hereda la visibilidad del área. */
  roles?: readonly Role[];
}
```

Área partidas:

```tsx
  {
    id: "partidas",
    label: "Partidas",
    role: ["Operador", "Administrador"],
    icon: Flag,
    items: [
      { label: "Partidas", path: "/partidas", icon: ListChecks },
      { label: "Nueva partida", path: "/partidas/crear", icon: Plus, roles: ["Operador"] }
    ]
  },
```

`areasForRoles` filtra también items:

```tsx
export function areasForRoles(roles: string[]): NavAreaDef[] {
  return NAV_AREAS.filter((area) => {
    const allowedRoles = typeof area.role === "string" ? [area.role] : area.role;
    return allowedRoles.some((role) => roles.includes(role));
  }).map((area) => ({
    ...area,
    items: area.items.filter(
      (item) => !item.roles || item.roles.some((role) => roles.includes(role))
    )
  }));
}
```

- [ ] **Step 4: Tests que fallan — rutas en App**

En `frontend/src/app/App.test.tsx`, siguiendo el patrón exacto de los tests existentes ("allows an admin to reach a partida history", línea ~99: `window.history.pushState` + render + `findByRole`/`findByTestId` con los mismos mocks del archivo), añadir:

```tsx
it("allows an admin to reach the partidas list in read-only mode", async () => {
  window.history.pushState({}, "", "/partidas");
  // usar el mismo helper de render + mock de sesión admin del archivo
  // la lista carga con el mock existente de getPartidas (o estado vacío)
  expect(await screen.findByTestId("lista-partidas")).toBeInTheDocument();
});

it("allows an admin to reach the sesion console", async () => {
  window.history.pushState({}, "", "/partidas/11111111-1111-1111-1111-111111111111/sesion");
  expect(await screen.findByTestId("sesion-operador")).toBeInTheDocument();
});
```

El test existente "keeps partida creation unavailable to an admin without the operator role" (línea ~148) debe seguir pasando sin cambios — `partidas/crear` sigue `need="Operador"`.

- [ ] **Step 5: Correr y ver fallar**

Run: `cd frontend && npx vitest run src/app/App.test.tsx`
Expected: FAIL (admin rebotado al landing por `need="Operador"`).

- [ ] **Step 6: Implementar rutas**

En `frontend/src/app/App.tsx`, cambiar SOLO el `need` de estas 3 rutas (`partidas/crear` NO se toca):

```tsx
path: "partidas",        → need={["Operador", "Administrador"]}
path: "partidas/:partidaId",        → need={["Operador", "Administrador"]}
path: "partidas/:partidaId/sesion", → need={["Operador", "Administrador"]}
```

(mismo formato que ya usa la ruta `partidas/:partidaId/historial`).

- [ ] **Step 7: Suite verde + gates**

Run: `cd frontend && npm test && npx tsc -b && npm run build`
Expected: PASS (≥185 tests), tsc limpio, build OK. Borrar artefactos generados si aparecen.

- [ ] **Step 8: Commit**

```bash
git add frontend/src/shell/navConfig.tsx frontend/src/shell/navConfig.test.tsx frontend/src/app/App.tsx frontend/src/app/App.test.tsx
git commit -m "feat(web): rutas y nav de partidas visibles para administrador (remediacion H1)"
```

---

### Task 2: Gating de acciones en lista y detalle (R1 páginas)

**Files:**
- Modify: `frontend/src/app/App.tsx` (elements de `PartidasListPage` y `PartidaDetailPage`)
- Modify: `frontend/src/features/partidas/PartidasListPage.tsx`
- Modify: `frontend/src/features/partidas/PartidaDetailPage.tsx`
- Test: `frontend/src/features/partidas/PartidasListPage.test.tsx`, `frontend/src/features/partidas/PartidaDetailPage.test.tsx` (archivos existentes; si el nombre difiere, buscar el test actual de cada página y extenderlo)

**Interfaces:**
- Consumes: rutas de Task 1.
- Produces: `PartidasListPage({ accessToken, puedeOperar }: { accessToken: string; puedeOperar: boolean })` y `PartidaDetailPage` ídem; `PartidaDetailContent` recibe `puedeOperar` por props.

- [ ] **Step 1: Tests que fallan**

En el test existente de cada página (extender con el patrón de render del archivo — las páginas ya se renderizan con mocks de API):

```tsx
it("oculta 'Nueva partida' cuando puedeOperar es false", async () => {
  renderPage({ puedeOperar: false }); // adaptar al helper del archivo
  expect(await screen.findByTestId("lista-partidas")).toBeInTheDocument();
  expect(screen.queryByTestId("btn-nueva-partida")).toBeNull();
});

it("muestra 'Nueva partida' cuando puedeOperar es true", async () => {
  renderPage({ puedeOperar: true });
  expect(await screen.findByTestId("btn-nueva-partida")).toBeInTheDocument();
});
```

Y en el de detalle:

```tsx
it("oculta 'Publicar y operar' cuando puedeOperar es false", async () => {
  renderPage({ puedeOperar: false });
  expect(await screen.findByText(/historial de eventos/i)).toBeInTheDocument();
  expect(screen.queryByTestId("btn-publicar-operar")).toBeNull();
});
```

Los tests existentes de cada página se actualizan pasando `puedeOperar={true}` (comportamiento idéntico al actual).

- [ ] **Step 2: Correr y ver fallar**

Run: `cd frontend && npx vitest run src/features/partidas/PartidasListPage.test.tsx src/features/partidas/PartidaDetailPage.test.tsx`
Expected: FAIL (prop inexistente / botón siempre presente).

- [ ] **Step 3: Implementar**

`PartidasListPage.tsx` — añadir la prop y condicionar el botón (líneas ~49-55):

```tsx
export function PartidasListPage({ accessToken, puedeOperar }: PartidasListPageProps) {
```

```tsx
{puedeOperar ? (
  <button
    type="button"
    data-testid="btn-nueva-partida"
    onClick={() => navigate("/partidas/crear")}
  >
    Nueva partida
  </button>
) : null}
```

`PartidaDetailPage.tsx` — `PartidaDetailPage({ accessToken, puedeOperar })`, pasar `puedeOperar` a `PartidaDetailContent` (llamada en línea ~71) y condicionar el botón (líneas ~125-132):

```tsx
{puedeOperar ? (
  <button
    type="button"
    data-testid="btn-publicar-operar"
    disabled={publicando}
    onClick={() => void onPublicar()}
  >
    Publicar y operar
  </button>
) : null}
```

`App.tsx` — calcular una vez dentro del componente que arma las rutas (donde ya existe `roles`):

```tsx
const puedeOperar = roles.includes("Operador");
```

y pasar `puedeOperar={puedeOperar}` a `<PartidasListPage />` y `<PartidaDetailPage />`.

- [ ] **Step 4: Suite verde + gates**

Run: `cd frontend && npm test && npx tsc -b && npm run build`
Expected: PASS. tsc obligará a actualizar cualquier otro callsite de estas páginas (tests incluidos).

- [ ] **Step 5: Commit**

```bash
git add frontend/src/app/App.tsx frontend/src/features/partidas/PartidasListPage.tsx frontend/src/features/partidas/PartidaDetailPage.tsx <tests tocados>
git commit -m "feat(web): lista y detalle de partidas en modo observador para admin (remediacion H1)"
```

---

### Task 3: Gating de la consola de sesión y paneles (R1 consola)

**Files:**
- Modify: `frontend/src/app/App.tsx` (element de `SesionOperadorPage`)
- Modify: `frontend/src/features/partidas/SesionOperadorPage.tsx`
- Modify: `frontend/src/features/partidas/TriviaRuntimePanel.tsx`
- Modify: `frontend/src/features/partidas/BdtRuntimePanel.tsx`
- Test: tests existentes de `SesionOperadorPage`, `TriviaRuntimePanel`, `BdtRuntimePanel` (extender)

**Interfaces:**
- Consumes: `puedeOperar` calculada en `App.tsx` (Task 2).
- Produces: `SesionOperadorPage({ accessToken, puedeOperar })`; `VistaCtx.puedeOperar: boolean`; `TriviaRuntimePanelProps.puedeOperar: boolean`; `BdtRuntimePanelProps.puedeOperar: boolean`. `PistasPanel` NO cambia de firma (se condiciona su render).

- [ ] **Step 1: Tests que fallan**

Extender los tests existentes de los 3 componentes (mismo helper de render de cada archivo):

```tsx
// SesionOperadorPage (vista lobby con modo inicio Manual):
it("oculta 'Iniciar ahora' cuando puedeOperar es false", async () => { /* render con puedeOperar:false */ 
  expect(screen.queryByTestId("btn-iniciar")).toBeNull();
});

// TriviaRuntimePanel:
it("oculta avanzar y finalizar cuando puedeOperar es false", async () => {
  expect(screen.queryByTestId("btn-avanzar-pregunta")).toBeNull();
  expect(screen.queryByTestId("btn-finalizar-juego")).toBeNull();
});

// BdtRuntimePanel:
it("oculta avanzar y finalizar cuando puedeOperar es false", async () => {
  expect(screen.queryByTestId("btn-avanzar-etapa")).toBeNull();
  expect(screen.queryByTestId("btn-finalizar-juego")).toBeNull();
});

// SesionOperadorPage (vista iniciada con juego BDT activo):
it("oculta el panel de pistas cuando puedeOperar es false", async () => {
  expect(screen.queryByText(/pista/i)).toBeNull(); // ajustar al texto/testid real del PistasPanel
});
```

Los tests existentes pasan `puedeOperar={true}` (o `true` en el ctx) y quedan idénticos.

- [ ] **Step 2: Correr y ver fallar**

Run: `cd frontend && npx vitest run src/features/partidas/`
Expected: FAIL en los 4 casos nuevos.

- [ ] **Step 3: Implementar**

`SesionOperadorPage.tsx`:
- `interface Props { accessToken: string; puedeOperar: boolean; }` y destructurar en la firma.
- `VistaCtx` gana `puedeOperar: boolean`; incluirlo en el objeto pasado a `renderVista` (línea ~172).
- Botón iniciar (línea ~325): `{mostrarManual && ctx.puedeOperar ? ( <button ... data-testid="btn-iniciar" ...> ) : null}`.
- `TriviaRuntimePanel` y `BdtRuntimePanel` reciben `puedeOperar={ctx.puedeOperar}` (líneas ~392 y ~406).
- PistasPanel (línea ~415): `{ctx.puedeOperar ? <PistasPanel partidaId={partidaId} accessToken={accessToken} /> : null}` — `GeoMapPanel` queda SIEMPRE visible (observador ve ubicaciones).

`TriviaRuntimePanel.tsx` / `BdtRuntimePanel.tsx`:
- `puedeOperar: boolean` en sus `Props` + destructuring.
- Envolver los botones `btn-avanzar-pregunta` / `btn-avanzar-etapa` / ambos `btn-finalizar-juego` en `{puedeOperar ? ... : null}` sin tocar nada más del JSX (rankings, countdown y estado quedan visibles).

`App.tsx`: `<SesionOperadorPage accessToken={token} puedeOperar={puedeOperar} />`.

- [ ] **Step 4: Suite verde + gates**

Run: `cd frontend && npm test && npx tsc -b && npm run build`
Expected: PASS (todos los archivos que instancian los paneles actualizados por tsc).

- [ ] **Step 5: Commit**

```bash
git add frontend/src/app/App.tsx frontend/src/features/partidas/SesionOperadorPage.tsx frontend/src/features/partidas/TriviaRuntimePanel.tsx frontend/src/features/partidas/BdtRuntimePanel.tsx <tests tocados>
git commit -m "feat(web): consola de sesion en modo observador para admin (remediacion H1)"
```

---

### Task 4: Contratos y scripts (R2 + R3 + R4)

**Files:**
- Modify: `contracts/http/gateway-api.md` (tabla "Route matrix (SP-5a)" + notas)
- Modify: `contracts/events/puntuaciones-events.md` (tabla Event Registry + Rule)
- Modify: `mobile/run-local.sh` (líneas 30-31), `mobile/run-local.ps1` (líneas 53-54 + definiciones `$BdtPort`/`$TriviaPort` si quedan sin uso)

**Interfaces:** ninguna (docs/scripts).

- [ ] **Step 1: R2 — fila en la matriz del gateway**

En `contracts/http/gateway-api.md`, insertar en la tabla, ANTES de la fila `/identity/teams/{**catch-all}`:

```markdown
| `/identity/teams` (exacto, solo `GET`) | 0 | `OperadorOAdministrador` | Identity |
```

Y añadir a las Notas:

```markdown
- `/identity/teams` exacto con `Methods: ["GET"]` (Order 0) intercepta el listado para la
  consola web antes del catch-all de Participante (Order 1); `GET /identity/teams/mine` y
  `POST /identity/teams` siguen cayendo en la ruta de Participante. Detalle del endpoint en
  `identity-api.md` §"Teams listing for the web console".
```

- [ ] **Step 2: R3 — índice de eventos de Puntuaciones**

En `contracts/events/puntuaciones-events.md`, en las 4 filas de la tabla, reemplazar `Defined by SDD` → consumidor real y `Payload not registered` → registro:

```markdown
| `PuntajeTriviaIncrementado` | A participant or team receives direct Trivia score from a correct answer. | Clientes web/mobile vía SignalR | Registrado — ver `contracts/http/puntuaciones-api.md` §"SignalR — ranking en vivo (SP-4c)" |
| `RankingTriviaActualizado` | The Trivia native ranking changes. | Clientes web/mobile vía SignalR | Registrado — ídem |
| `RankingBDTActualizado` | The BDT native ranking changes by accumulated points from won stages. | Clientes web/mobile vía SignalR | Registrado — ídem |
| `RankingConsolidadoCalculado` | The consolidated partida ranking is computed on finish. | Clientes web/mobile vía SignalR | Registrado — ídem |
```

Y en `## Status` / `## Rule`, sustituir la afirmación de "payloads require a current-doctrine SDD" por una nota de que los payloads SignalR quedaron registrados por SP-4c en `puntuaciones-api.md` (una sola fuente, sin duplicarlos aquí).

- [ ] **Step 3: R4 — scripts mobile**

`mobile/run-local.sh`: borrar exactamente las líneas:

```bash
EXPO_PUBLIC_BDT_API_BASE_URL=http://${IP}:${BDT_PORT:-5016}
EXPO_PUBLIC_TRIVIA_API_BASE_URL=http://${IP}:${TRIVIA_PORT:-5015}
```

`mobile/run-local.ps1`: borrar las líneas 53-54 (`EXPO_PUBLIC_BDT_API_BASE_URL=...`, `EXPO_PUBLIC_TRIVIA_API_BASE_URL=...`) y, si `$BdtPort`/`$TriviaPort` quedan sin ningún otro uso en el archivo, borrar también sus definiciones.

- [ ] **Step 4: Verificar**

Run: `grep -n "TRIVIA_API\|BDT_API" mobile/run-local.sh mobile/run-local.ps1` → sin resultados.
Run: `cd mobile && npm test && npm run typecheck` → 88 pass + limpio (smoke; no se tocó `mobile/src`).

- [ ] **Step 5: Commit**

```bash
git add contracts/http/gateway-api.md contracts/events/puntuaciones-events.md mobile/run-local.sh mobile/run-local.ps1
git commit -m "docs(contratos)+chore(mobile): matriz gateway, indice eventos puntuaciones y scripts sin vars legacy (remediacion H2-H4)"
```

---

### Cierre (lo ejecuta el controlador, no un subagente)

- Gates completos en HEAD: web `npm test` + `tsc -b` + `build`; mobile `npm test` + `typecheck`.
- `docs/04-sdd/traceability-matrix.md`: fila de la remediación (H1-H4 → commits).
- Anexo "Remediación aplicada" en `docs/04-sdd/auditorias/2026-07-11-informe-conformidad-bloques2-3.md` (hallazgo → commit; el veredicto original no se reescribe).
- Ledger `.git/sdd/progress.md` + review final whole-branch (opus) del rango de la remediación.
