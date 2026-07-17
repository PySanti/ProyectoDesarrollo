# Bloque 3a — Rankings en vivo por push SignalR (clientes) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Web y mobile consumen el hub `puntuaciones/hubs/ranking` (ya operativo en backend) para actualizar rankings intra-pregunta/etapa por push, de forma ADITIVA sobre el GET existente.

**Architecture:** Web: factory `rankingHub.ts` + hook `useRankingHub` (espejo de `useSesionHub`) montado en `SesionOperadorPage`, que baja payloads como props `rankingPush`/`consolidadoPush` a los paneles existentes; cada panel aplica el payload sobre su mismo estado de ranking cuando el `juegoId` coincide. Mobile: `rankingHub.js` (espejo de `sesionHub.js`) como segundo hub en `PartidaLiveScreen`, mismo esquema de props. Ningún GET se retira.

**Tech Stack:** `@microsoft/signalr@^8` (ya instalado en ambos clientes), React 18 / React Native, vitest + `node --test`.

## Global Constraints

- Spec: `docs/superpowers/specs/2026-07-10-bloque3a-rankings-push-clientes-design.md`.
- Solo `frontend/` y `mobile/` (+docs en T3). PROHIBIDO tocar backend, gateway o `contracts/` (el hub ya existe).
- Contrato hub (verbatim de `contracts/http/puntuaciones-api.md` §SP-4c): URL `{gw}/puntuaciones/hubs/ranking`; token por query `access_token` (accessTokenFactory); `SuscribirAPartida(partidaId)` puede lanzar `HubException("Partida no proyectada.")` → catch silencioso; `DesuscribirDePartida(partidaId)` al desmontar; mensajes `RankingTriviaActualizado` y `RankingBDTActualizado` con el shape de `GET .../juegos/{juegoId}/ranking` (`{juegoId, tipoJuego, generadoEn, entradas[]}`), `RankingConsolidadoCalculado` con el shape de `GET .../ranking-consolidado` (`{partidaId, generadoEn, entradas[]}`).
- **Push aditivo:** NO retirar ningún GET inicial ni GET-en-señal existente.
- Gates: `cd frontend && npm test` (baseline: **157**) + `npx tsc -b` (limpio; borrar artefactos `tsconfig*.tsbuildinfo`/`vite.config.js|d.ts`/`vitest.config.js|d.ts` si aparecen) + `npm run build`; `cd mobile && npm test` (baseline: **74**) + `npm run typecheck`.
- Commits con trailer: `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- PROHIBIDO a implementers: `git stash/reset/checkout/restore/clean`. Solo `git add <paths exactos>` + `git commit`.

---

### Task 1: Web — `rankingHub.ts` + `useRankingHub` + wiring de paneles

**Files:**
- Create: `frontend/src/api/rankingHub.ts`
- Create: `frontend/src/features/partidas/useRankingHub.ts`
- Create: `frontend/src/features/partidas/useRankingHub.test.ts`
- Modify: `frontend/src/features/partidas/TriviaRuntimePanel.tsx` (prop `rankingPush` + effect)
- Modify: `frontend/src/features/partidas/BdtRuntimePanel.tsx` (prop `rankingPush` + effect, idéntico)
- Modify: `frontend/src/features/partidas/ConsolidadoPanel.tsx` (prop `consolidadoPush` + effect)
- Modify: `frontend/src/features/partidas/SesionOperadorPage.tsx` (montar hook + threading)

**Interfaces:**
- Consumes: `RankingJuegoDto`/`RankingConsolidadoDto` de `../../api/puntuacionesApi` (existentes); patrón `useSesionHub`/`crearSesionHub` (existentes).
- Produces: `crearRankingHub(accessToken): HubConnection` y `rankingHubUrl(): string` (api/rankingHub.ts); `useRankingHub(partidaId, accessToken, {onRankingJuego?, onConsolidado?})`; props nuevas `rankingPush?: RankingJuegoDto | null` (ambos runtime panels) y `consolidadoPush?: RankingConsolidadoDto | null` (ConsolidadoPanel). T3 verifica E2E.

- [ ] **Step 1: Factory** — crear `frontend/src/api/rankingHub.ts`:

```ts
// Fabrica delgada de la conexion SignalR al hub de rankings (Puntuaciones), via gateway.
import { HubConnection, HubConnectionBuilder } from "@microsoft/signalr";

function resolveBaseUrl(): string {
  const value = import.meta.env.VITE_GATEWAY_BASE_URL as string | undefined;
  if (!value) {
    throw new Error("Missing VITE_GATEWAY_BASE_URL environment variable.");
  }
  return value.replace(/\/$/, "");
}

export function rankingHubUrl(): string {
  return `${resolveBaseUrl()}/puntuaciones/hubs/ranking`;
}

export function crearRankingHub(accessToken: string): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(rankingHubUrl(), { accessTokenFactory: () => accessToken })
    .withAutomaticReconnect()
    .build();
}
```

- [ ] **Step 2: Test del hook (falla)** — crear `frontend/src/features/partidas/useRankingHub.test.ts`:

```ts
import { afterEach, describe, expect, it, vi } from "vitest";
import { renderHook } from "@testing-library/react";
import { useRankingHub } from "./useRankingHub";
import { crearRankingHub } from "../../api/rankingHub";

vi.mock("../../api/rankingHub", () => ({ crearRankingHub: vi.fn() }));

function fakeConnection() {
  const handlers: Record<string, (p: unknown) => void> = {};
  return {
    handlers,
    on: vi.fn((event: string, cb: (p: unknown) => void) => {
      handlers[event] = cb;
    }),
    onreconnected: vi.fn(),
    start: vi.fn().mockResolvedValue(undefined),
    stop: vi.fn().mockResolvedValue(undefined),
    invoke: vi.fn().mockResolvedValue(undefined)
  };
}

describe("useRankingHub", () => {
  afterEach(() => vi.clearAllMocks());

  it("suscribe a la partida y enruta ambos mensajes de juego a onRankingJuego", async () => {
    const conn = fakeConnection();
    vi.mocked(crearRankingHub).mockReturnValue(conn as never);
    const onRankingJuego = vi.fn();
    const onConsolidado = vi.fn();

    const { unmount } = renderHook(() =>
      useRankingHub("p1", "tok", { onRankingJuego, onConsolidado })
    );
    await Promise.resolve();
    await Promise.resolve();

    expect(conn.start).toHaveBeenCalled();
    expect(conn.invoke).toHaveBeenCalledWith("SuscribirAPartida", "p1");

    const payload = { juegoId: "j1", tipoJuego: "Trivia", generadoEn: "t", entradas: [] };
    conn.handlers["RankingTriviaActualizado"](payload);
    expect(onRankingJuego).toHaveBeenCalledWith(payload);
    conn.handlers["RankingBDTActualizado"]({ ...payload, tipoJuego: "BusquedaDelTesoro" });
    expect(onRankingJuego).toHaveBeenCalledTimes(2);

    const consolidado = { partidaId: "p1", generadoEn: "t", entradas: [] };
    conn.handlers["RankingConsolidadoCalculado"](consolidado);
    expect(onConsolidado).toHaveBeenCalledWith(consolidado);

    unmount();
    expect(conn.invoke).toHaveBeenCalledWith("DesuscribirDePartida", "p1");
    expect(conn.stop).toHaveBeenCalled();
  });

  it("partidaId vacío no crea conexión", () => {
    renderHook(() => useRankingHub("", "tok", {}));
    expect(crearRankingHub).not.toHaveBeenCalled();
  });
});
```

- [ ] **Step 3: Verificar que falla** — Run: `cd frontend && npx vitest run src/features/partidas/useRankingHub.test.ts`. Expected: FAIL (módulo `./useRankingHub` no existe).

- [ ] **Step 4: Hook** — crear `frontend/src/features/partidas/useRankingHub.ts`:

```ts
// Hub de rankings (Puntuaciones, SP-4c): push aditivo sobre los GET existentes.
import { useEffect, useRef } from "react";
import { crearRankingHub } from "../../api/rankingHub";
import type { RankingConsolidadoDto, RankingJuegoDto } from "../../api/puntuacionesApi";

export interface RankingHubHandlers {
  onRankingJuego?: (payload: RankingJuegoDto) => void;
  onConsolidado?: (payload: RankingConsolidadoDto) => void;
}

export function useRankingHub(
  partidaId: string,
  accessToken: string,
  handlers: RankingHubHandlers
): void {
  const handlersRef = useRef(handlers);
  handlersRef.current = handlers;

  useEffect(() => {
    if (!partidaId) return;

    const connection = crearRankingHub(accessToken);
    let active = true;

    // "Partida no proyectada." es esperado si la proyección aún no llegó: el GET cubre.
    const suscribir = () => connection.invoke("SuscribirAPartida", partidaId).catch(() => {});

    connection.on("RankingTriviaActualizado", (p) => handlersRef.current.onRankingJuego?.(p));
    connection.on("RankingBDTActualizado", (p) => handlersRef.current.onRankingJuego?.(p));
    connection.on("RankingConsolidadoCalculado", (p) => handlersRef.current.onConsolidado?.(p));
    connection.onreconnected(() => {
      if (active) void suscribir();
    });

    connection
      .start()
      .then(() => {
        if (active) void suscribir();
      })
      .catch(() => {});

    return () => {
      active = false;
      connection.invoke("DesuscribirDePartida", partidaId).catch(() => {});
      void connection.stop().catch(() => {});
    };
  }, [partidaId, accessToken]);
}
```

- [ ] **Step 5: Verificar hook verde** — Run: `cd frontend && npx vitest run src/features/partidas/useRankingHub.test.ts`. Expected: PASS 2/2.

- [ ] **Step 6: Prop push en los tres paneles** —
En `frontend/src/features/partidas/TriviaRuntimePanel.tsx`: añadir a `TriviaRuntimePanelProps` el campo `rankingPush?: RankingJuegoDto | null;`, incluir `rankingPush` en la destructuración de `props`, y añadir tras el effect existente de carga:

```tsx
  // Push SP-4c aditivo: aplica el ranking recibido por hub si es de este juego.
  useEffect(() => {
    if (rankingPush && rankingPush.juegoId === juegoId) {
      setRanking(rankingPush);
    }
  }, [rankingPush, juegoId]);
```

En `frontend/src/features/partidas/BdtRuntimePanel.tsx`: exactamente los mismos tres cambios (prop en la interface de props, destructuración, mismo effect — el estado se llama igual, `setRanking`).

En `frontend/src/features/partidas/ConsolidadoPanel.tsx`: la firma pasa a

```tsx
export function ConsolidadoPanel({
  partidaId,
  accessToken,
  consolidadoPush
}: {
  partidaId: string;
  accessToken: string;
  consolidadoPush?: RankingConsolidadoDto | null;
}) {
```

y tras el effect existente añadir:

```tsx
  // Push SP-4c: el consolidado difundido al finalizar pinta sin esperar el retry del GET.
  useEffect(() => {
    if (consolidadoPush) {
      setEstado({ status: "ok", ranking: consolidadoPush });
    }
  }, [consolidadoPush]);
```

- [ ] **Step 7: Montaje en `SesionOperadorPage.tsx`** —
Imports: añadir `import { useRankingHub } from "./useRankingHub";` y ampliar el import de puntuacionesApi con `type RankingJuegoDto, type RankingConsolidadoDto` (si no están).
En el cuerpo de `SesionOperadorPage`, junto a los estados existentes:

```tsx
  const [rankingPush, setRankingPush] = useState<RankingJuegoDto | null>(null);
  const [consolidadoPush, setConsolidadoPush] = useState<RankingConsolidadoDto | null>(null);
```

Tras la llamada existente `useSesionHub(partidaId ?? "", accessToken, handlers);` añadir:

```tsx
  useRankingHub(partidaId ?? "", accessToken, {
    onRankingJuego: setRankingPush,
    onConsolidado: setConsolidadoPush
  });
```

Threading: añadir `rankingPush: RankingJuegoDto | null;` a `VistaCtx` y a `IniciadaViewProps`; pasarlo en el objeto ctx donde se construye (donde se pasan `refetchSignal`, `ubicaciones`, etc.), en el `<IniciadaView ... rankingPush={ctx.rankingPush} />` del case `"iniciada"`, y de `IniciadaView` a ambos paneles como `rankingPush={rankingPush}`. En el case `"terminada"`, pasar `consolidadoPush` — como `renderVista` es función suelta, añadir también `consolidadoPush: RankingConsolidadoDto | null;` a `VistaCtx` y usar `<ConsolidadoPanel partidaId={ctx.partidaId} accessToken={ctx.accessToken} consolidadoPush={ctx.consolidadoPush} />`.

- [ ] **Step 8: Gates web** — Run: `cd frontend && npm test` (159 = 157 + 2) y `npx tsc -b` (limpio; borrar artefactos si aparecen) y `npm run build` (PASS).

- [ ] **Step 9: Commit**

```bash
git add frontend/src/api/rankingHub.ts frontend/src/features/partidas/useRankingHub.ts frontend/src/features/partidas/useRankingHub.test.ts frontend/src/features/partidas/TriviaRuntimePanel.tsx frontend/src/features/partidas/BdtRuntimePanel.tsx frontend/src/features/partidas/ConsolidadoPanel.tsx frontend/src/features/partidas/SesionOperadorPage.tsx
git commit -m "feat(web): rankings en vivo por push del hub de puntuaciones SP-4c (bloque 3a)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 2: Mobile — `rankingHub.js` + wiring del live

**Files:**
- Create: `mobile/src/features/partidas/rankingHub.js`
- Create: `mobile/tests/rankingHub.test.js`
- Modify: `mobile/src/features/partidas/PartidaLiveScreen.tsx`
- Modify: `mobile/src/features/partidas/TriviaPlayPanel.tsx` (prop `rankingPush` + effect)
- Modify: `mobile/src/features/partidas/BdtPlayPanel.tsx` (prop `rankingPush` + effect)

**Interfaces:**
- Consumes: patrón `sesionHub.js` (`crearSesionHub(gatewayBaseUrl, accessToken)`); tipo `RankingEntrada` de `./liveShared`.
- Produces: `crearRankingHub(gatewayBaseUrl, accessToken)` y `rankingHubUrl(gatewayBaseUrl)`; prop `rankingPush: {juegoId: string; entradas: RankingEntrada[]} | null` en ambos panels. T3 verifica E2E.

- [ ] **Step 1: Test que falla** — crear `mobile/tests/rankingHub.test.js`:

```js
import test from "node:test";
import assert from "node:assert/strict";
import { rankingHubUrl } from "../src/features/partidas/rankingHub.js";

test("rankingHubUrl apunta al hub de puntuaciones via gateway sin doble slash", () => {
  assert.equal(rankingHubUrl("http://gw:5080/"), "http://gw:5080/puntuaciones/hubs/ranking");
  assert.equal(rankingHubUrl("http://gw:5080"), "http://gw:5080/puntuaciones/hubs/ranking");
});
```

- [ ] **Step 2: Verificar que falla** — Run: `cd mobile && node --test tests/rankingHub.test.js`. Expected: FAIL (módulo no existe).

- [ ] **Step 3: Implementar** — crear `mobile/src/features/partidas/rankingHub.js`:

```js
// Hub de rankings de Puntuaciones (SP-4c) via gateway. El caller arranca/detiene la conexion.
import { HubConnectionBuilder } from "@microsoft/signalr";

export function rankingHubUrl(gatewayBaseUrl) {
  return `${gatewayBaseUrl.replace(/\/$/, "")}/puntuaciones/hubs/ranking`;
}

export function crearRankingHub(gatewayBaseUrl, accessToken) {
  return new HubConnectionBuilder()
    .withUrl(rankingHubUrl(gatewayBaseUrl), { accessTokenFactory: () => accessToken })
    .withAutomaticReconnect()
    .build();
}
```

- [ ] **Step 4: Verificar verde** — Run: `cd mobile && node --test tests/rankingHub.test.js` (1/1).

- [ ] **Step 5: Prop push en los paneles** —
En `mobile/src/features/partidas/TriviaPlayPanel.tsx`: añadir a `Props` el campo `rankingPush: { juegoId: string; entradas: RankingEntrada[] } | null;`, incluirlo en la destructuración, y añadir tras el effect de reset:

```tsx
  // Push SP-4c aditivo: ranking en vivo sin esperar señal de cierre.
  useEffect(() => {
    if (rankingPush && rankingPush.juegoId === juegoId) {
      setEntradas(rankingPush.entradas);
    }
  }, [rankingPush, juegoId]);
```

En `mobile/src/features/partidas/BdtPlayPanel.tsx`: exactamente los mismos tres cambios (mismo nombre de estado `setEntradas`).

- [ ] **Step 6: Segundo hub en `PartidaLiveScreen.tsx`** —
Imports: `import { crearRankingHub } from "./rankingHub.js";`
Estados nuevos junto a los existentes:

```tsx
  const [rankingPush, setRankingPush] = useState<{ juegoId: string; entradas: RankingEntrada[] } | null>(null);
```

(`RankingEntrada` ya se importa de `./liveShared` — ampliar ese import si hace falta.)
Nuevo `useEffect` después del efecto del hub de sesión (patrón idéntico: handlers antes de `start`, catch silencioso, cleanup):

```tsx
  // Hub de rankings (SP-4c): push aditivo; el GET existente sigue siendo la fuente recuperable.
  useEffect(() => {
    const hub = crearRankingHub(apiBaseUrl, token);
    const aplicarRankingJuego = (p: { juegoId?: string; entradas?: unknown[] }) => {
      if (p?.juegoId && Array.isArray(p.entradas)) {
        setRankingPush({ juegoId: p.juegoId, entradas: p.entradas as never });
      }
    };
    hub.on("RankingTriviaActualizado", aplicarRankingJuego);
    hub.on("RankingBDTActualizado", aplicarRankingJuego);
    hub.on("RankingConsolidadoCalculado", (p: { entradas?: ConsolidadoEntrada[] }) => {
      if (Array.isArray(p?.entradas)) setConsolidado(p.entradas);
    });
    hub
      .start()
      .then(() => hub.invoke("SuscribirAPartida", partidaId))
      .catch(() => {});
    return () => {
      void hub.stop().catch(() => {});
    };
  }, [apiBaseUrl, token, partidaId]);
```

Render: pasar `rankingPush={rankingPush}` a `<TriviaPlayPanel …>` y `<BdtPlayPanel …>`.
Nota: `setConsolidado` ya existe (fase finalizada); el push lo puebla anticipadamente y el GET + Reintentar quedan como recuperación. El shape del push consolidado usa `puntosTotales`/`juegosGanados` igual que el GET — el render de fase finalizada ya mapea `ConsolidadoEntrada`, así que el push entrega directamente `p.entradas` (mismo shape del GET, sin mapear).

- [ ] **Step 7: Gates mobile** — Run: `cd mobile && npm test` (75 = 74 + 1) y `npm run typecheck` (limpio).

- [ ] **Step 8: Commit**

```bash
git add mobile/src/features/partidas/rankingHub.js mobile/tests/rankingHub.test.js mobile/src/features/partidas/PartidaLiveScreen.tsx mobile/src/features/partidas/TriviaPlayPanel.tsx mobile/src/features/partidas/BdtPlayPanel.tsx
git commit -m "feat(mobile): rankings en vivo por push del hub de puntuaciones SP-4c (bloque 3a)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 3: Gate E2E + traceability (controller — NO subagente)

**Files:**
- Modify: `docs/04-sdd/traceability-matrix.md` (fila 3a tras la 2f)
- Modify: `GUIA-LEVANTAMIENTO.md` (nota rankings en vivo)

**Interfaces:**
- Consumes: todo lo anterior a HEAD; stack completo; `get-token.sh` PKCE del scratchpad; smoke node vía `createRequire` CJS de `mobile/node_modules/@microsoft/signalr`.
- Produces: cierre del slice (fila traceability + nota GUIA + ledger).

- [ ] **Step 1: Suites en HEAD** — frontend 159 + tsc + build; mobile 75 + typecheck.
- [ ] **Step 2: Stack + tokens** — infra + 4 servicios + gateway; tokens operador/participante (verificar puertos con `ss -tlnp`, NO `/dev/tcp`).
- [ ] **Step 3: Smoke hub rankings** — cliente node conectado a `{gw}/puntuaciones/hubs/ranking` con token participante, `SuscribirAPartida`: (a) suscripción a partida NO proyectada → error de hub capturado sin romper (`HubException "Partida no proyectada."`); (b) partida Trivia real: al responder correcto llega `RankingTriviaActualizado {juegoId, entradas[{puntos}]}` **antes** de cerrar/avanzar pregunta; al finalizar la partida llega `RankingConsolidadoCalculado {entradas[{juegosGanados, puntosTotales}]}`.
- [ ] **Step 4: Docs** — fila 3a en traceability (patrón filas previas; commits T1-T2; evidencia smoke) + GUIA: nota breve "Bloque 3a: rankings en vivo por push (SP-4c) en consola web y live mobile; GET queda como fuente recuperable".
- [ ] **Step 5: Commit**

```bash
git add docs/04-sdd/traceability-matrix.md GUIA-LEVANTAMIENTO.md
git commit -m "docs(bloque3a): traceability fila 3a + nota GUIA rankings en vivo" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```
