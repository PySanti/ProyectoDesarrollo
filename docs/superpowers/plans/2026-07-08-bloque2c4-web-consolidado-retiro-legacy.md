# Bloque 2c-4 — Ranking consolidado en vista terminada + retiro legacy (web operador) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cerrar el Bloque 2c: mostrar el ranking consolidado de la partida en la vista `terminada` de la consola, extraer el módulo compartido de runtime (Countdown/RankingView), y retirar las páginas legacy de operación que la consola nueva reemplaza.

**Architecture:** Solo frontend web. La vista `terminada` de `SesionOperadorPage` monta un `ConsolidadoPanel` nuevo que hace un GET puntual al endpoint de consolidado de Puntuaciones con reintento corto (cubre el lag de proyección). En paralelo se extrae a `runtimeShared.tsx` la lógica hoy triplicada (Countdown, RankingView, formatTiempo) y se retiran las páginas/APIs legacy `features/trivia` + `features/bdt`.

**Tech Stack:** React 18 + Vite + TypeScript, vitest + @testing-library/react, react-router-dom.

## Global Constraints

- **Sin cambios de contrato, regla de negocio ni HU.** Reconstrucción visual/IA solamente.
- **No tocar** `label`/`id`/`data-testid`/roles ARIA de los que dependen tests existentes. Testids nuevos se añaden; los de Trivia/BDT/inicio se preservan **exactos**.
- Gate typecheck real: **`npx tsc -b`** (bare `tsc --noEmit` es NO-OP: root tsconfig tiene `files:[]` + references). `tsc -b` deja artifacts untracked (`tsconfig.app.tsbuildinfo`, `tsconfig.node.tsbuildinfo`, `vite.config.js/.d.ts`, `vitest.config.js/.d.ts`) → **borrarlos** tras verificar.
- Cada commit termina con trailer: `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- Identidad de competidor = GUID solamente (contrato no entrega nombres) → mostrar `slice(0, 8)`.
- Subagents PROHIBIDO usar `git stash/reset/checkout/restore/clean`. Solo `git add <ruta exacta>`, archivo por archivo.
- `npm test` = `vitest run` desde `frontend/`. Baseline previa a T1: **150/150 verde, `tsc -b` limpio** (HEAD eb60625).

---

### Task 1: `getRankingConsolidado` en `puntuacionesApi.ts`

**Files:**
- Modify: `frontend/src/api/puntuacionesApi.ts` (añadir tipos + función; patrón inline-fetch existente, NO hay helper `request<T>` en este módulo)
- Test: `frontend/src/api/puntuacionesApi.test.ts` (añadir casos)

**Interfaces:**
- Consumes: nada nuevo (reusa `PuntuacionesApiError` y `resolveBaseUrl` ya en el módulo).
- Produces:
  - `RankingConsolidadoEntradaDto { posicion: number; competidorId: string; tipoCompetidor: "Participante" | "Equipo"; juegosGanados: number; puntosTotales: number; tiempoTotalMs: number }`
  - `RankingConsolidadoDto { partidaId: string; generadoEn: string; entradas: RankingConsolidadoEntradaDto[] }`
  - `getRankingConsolidado(partidaId: string, accessToken: string, fetchImpl?: typeof fetch): Promise<RankingConsolidadoDto>`

- [ ] **Step 1: Escribir tests que fallan**

Añadir al final del `describe("puntuacionesApi", …)` en `frontend/src/api/puntuacionesApi.test.ts` (el helper `okJson` y el `beforeEach`/`afterEach` de env ya existen arriba en el archivo). También importar la función nueva en la línea 2:

```ts
// línea 2 pasa a:
import { getRankingConsolidado, getRankingJuego, PuntuacionesApiError } from "./puntuacionesApi";
```

Casos nuevos:

```ts
  it("getRankingConsolidado hace GET autenticado al consolidado de la partida", async () => {
    const f = okJson({
      partidaId: "p1",
      generadoEn: "2026-07-08T12:00:00Z",
      entradas: [
        {
          posicion: 1,
          competidorId: "c1",
          tipoCompetidor: "Participante",
          juegosGanados: 2,
          puntosTotales: 45,
          tiempoTotalMs: 23456
        }
      ]
    });
    const r = await getRankingConsolidado("p1", "tok", f);
    expect(r.entradas[0].puntosTotales).toBe(45);
    expect(r.entradas[0].juegosGanados).toBe(2);
    expect(f.mock.calls[0][0]).toBe(
      "https://gw.example.test/puntuaciones/partidas/p1/ranking-consolidado"
    );
    expect((f.mock.calls[0][1].headers as Record<string, string>).Authorization).toBe("Bearer tok");
  });

  it("409 (partida no terminada) lanza PuntuacionesApiError con statusCode", async () => {
    const f = okJson({ message: "no terminada" }, 409);
    await expect(getRankingConsolidado("p1", "tok", f)).rejects.toMatchObject({ statusCode: 409 });
    await expect(getRankingConsolidado("p1", "tok", f)).rejects.toBeInstanceOf(PuntuacionesApiError);
  });

  it("consolidado 200 con entradas vacías es válido (terminada sin marcadores)", async () => {
    const f = okJson({ partidaId: "p1", generadoEn: "2026-07-08T12:00:00Z", entradas: [] });
    const r = await getRankingConsolidado("p1", "tok", f);
    expect(r.entradas).toEqual([]);
  });
```

- [ ] **Step 2: Correr tests, verificar que fallan**

Run: `cd frontend && npx vitest run src/api/puntuacionesApi.test.ts`
Expected: FAIL (`getRankingConsolidado` no exportado / no existe).

- [ ] **Step 3: Implementar**

Añadir al final de `frontend/src/api/puntuacionesApi.ts` (después de `getRankingJuego`), reflejando exactamente el patrón de esa función:

```ts
export interface RankingConsolidadoEntradaDto {
  posicion: number;
  competidorId: string;
  tipoCompetidor: "Participante" | "Equipo";
  juegosGanados: number;
  puntosTotales: number;
  tiempoTotalMs: number;
}

export interface RankingConsolidadoDto {
  partidaId: string;
  generadoEn: string;
  entradas: RankingConsolidadoEntradaDto[];
}

export async function getRankingConsolidado(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<RankingConsolidadoDto> {
  const response = await fetchImpl(
    `${resolveBaseUrl()}/puntuaciones/partidas/${partidaId}/ranking-consolidado`,
    { method: "GET", headers: { Authorization: `Bearer ${accessToken}` } }
  );
  const body = (await response.json().catch(() => ({}))) as RankingConsolidadoDto & {
    message?: string;
  };
  if (!response.ok) {
    const message = body.message ?? `Puntuaciones API error. Status=${response.status}`;
    throw new PuntuacionesApiError(message, response.status);
  }
  return body;
}
```

- [ ] **Step 4: Correr tests, verificar que pasan**

Run: `cd frontend && npx vitest run src/api/puntuacionesApi.test.ts`
Expected: PASS (5/5 en el archivo: 2 previos de getRankingJuego + 3 nuevos).

- [ ] **Step 5: Typecheck**

Run: `cd frontend && npx tsc -b`
Expected: exit 0. Borrar artifacts untracked que genere (`git status --short` no debe listar `*.tsbuildinfo`, `vite.config.js`, etc.).

- [ ] **Step 6: Commit**

```bash
cd frontend && git add src/api/puntuacionesApi.ts src/api/puntuacionesApi.test.ts
git commit -m "feat(web): puntuacionesApi getRankingConsolidado (bloque 2c4)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 2: Extraer `runtimeShared.tsx` (F-2) y recablear los 3 consumidores

**Files:**
- Create: `frontend/src/features/partidas/runtimeShared.tsx`
- Modify: `frontend/src/features/partidas/TriviaRuntimePanel.tsx` (quitar `RankingView`/`formatTiempo`/`Countdown` locales, importar del módulo)
- Modify: `frontend/src/features/partidas/BdtRuntimePanel.tsx` (idem)
- Modify: `frontend/src/features/partidas/SesionOperadorPage.tsx` (quitar `Countdown` local, importar del módulo)
- Tests: los existentes (`TriviaRuntimePanel.test.tsx`, `BdtRuntimePanel.test.tsx`, `SesionOperadorPage.test.tsx`) deben seguir verdes SIN cambios (el output renderizado — testids, textos, clases — es idéntico).

**Interfaces:**
- Consumes: `RankingJuegoDto` de `../../api/puntuacionesApi`.
- Produces (exports de `runtimeShared.tsx`):
  - `Countdown({ target, testid, expiredLabel?, muted? }): JSX.Element` — `expiredLabel` default `"Tiempo agotado"`; `muted` default `true`.
  - `RankingView({ ranking }: { ranking: RankingJuegoDto | null }): JSX.Element`
  - `formatTiempo(ms: number): string`

**Contexto crítico:** hoy hay 3 `Countdown` con diferencias SOLO presentacionales:
- Trivia: `<span className="muted" data-testid="pregunta-countdown">`, expira → `"Tiempo agotado"`.
- BDT: `<span className="muted" data-testid="etapa-countdown">`, expira → `"Tiempo agotado"`.
- Página (`SesionOperadorPage`): `<span data-testid="inicio-countdown">` (SIN `className`), expira → `"Iniciando…"`.

El `Countdown` compartido cubre las 3 vía props. `RankingView` + `formatTiempo` son byte-idénticos entre Trivia y BDT (extracción directa). La página NO usa `RankingView`.

- [ ] **Step 1: Crear `runtimeShared.tsx`**

```tsx
// Piezas compartidas del runtime del operador (Trivia + BDT + página de sesión):
// cuenta regresiva, tabla de ranking del juego, formato de tiempo mm:ss.
import { useEffect, useState } from "react";
import type { RankingJuegoDto } from "../../api/puntuacionesApi";

export function formatTiempo(ms: number): string {
  const mm = String(Math.floor(ms / 60000)).padStart(2, "0");
  const ss = String(Math.floor(ms / 1000) % 60).padStart(2, "0");
  return `${mm}:${ss}`;
}

export function Countdown({
  target,
  testid,
  expiredLabel = "Tiempo agotado",
  muted = true
}: {
  target: string;
  testid: string;
  expiredLabel?: string;
  muted?: boolean;
}) {
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, []);
  const remaining = Math.max(0, Math.floor((new Date(target).getTime() - now) / 1000));
  const mm = String(Math.floor(remaining / 60)).padStart(2, "0");
  const ss = String(remaining % 60).padStart(2, "0");
  return (
    <span className={muted ? "muted" : undefined} data-testid={testid}>
      {remaining > 0 ? `${mm}:${ss}` : expiredLabel}
    </span>
  );
}

export function RankingView({ ranking }: { ranking: RankingJuegoDto | null }) {
  if (!ranking?.entradas?.length) {
    return <p className="muted">Sin datos de ranking todavía.</p>;
  }
  return (
    <div className="table-wrap">
      <table aria-label="Ranking del juego" data-testid="ranking-juego">
        <thead>
          <tr>
            <th scope="col">Posición</th>
            <th scope="col">Competidor</th>
            <th scope="col">Puntos</th>
            <th scope="col">Tiempo</th>
            <th scope="col">Ganadas</th>
          </tr>
        </thead>
        <tbody>
          {ranking.entradas.map((entrada) => (
            <tr key={entrada.competidorId}>
              <td>{entrada.posicion}</td>
              <td>{entrada.competidorId.slice(0, 8)}</td>
              <td>{entrada.puntos}</td>
              <td>{formatTiempo(entrada.tiempoAcumuladoMs)}</td>
              <td>{entrada.unidadesGanadas}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
```

- [ ] **Step 2: Recablear `TriviaRuntimePanel.tsx`**

En `frontend/src/features/partidas/TriviaRuntimePanel.tsx`:
1. Añadir import tras la línea del import de `partidasApi` (línea 11):
   ```ts
   import { Countdown, RankingView } from "./runtimeShared";
   ```
2. Borrar las funciones locales `RankingView` (líneas 176-206), `formatTiempo` (208-212) y `Countdown` (214-228) — el bloque completo desde `function RankingView(` hasta el final del archivo.
3. En `PreguntaActivaView`, la llamada `<Countdown target={target} />` (línea 153) pasa a:
   ```tsx
   <Countdown target={target} testid="pregunta-countdown" />
   ```
   (`muted` default true y `expiredLabel` default "Tiempo agotado" reproducen el markup previo exacto.)

- [ ] **Step 3: Recablear `BdtRuntimePanel.tsx`**

En `frontend/src/features/partidas/BdtRuntimePanel.tsx`:
1. Añadir import tras el import de `puntuacionesApi` (línea 10):
   ```ts
   import { Countdown, RankingView } from "./runtimeShared";
   ```
2. Borrar las funciones locales `RankingView` (líneas 145-175), `formatTiempo` (177-181) y `Countdown` (183-197).
3. En `EtapaActivaView`, `<Countdown target={target} />` (línea 137) pasa a:
   ```tsx
   <Countdown target={target} testid="etapa-countdown" />
   ```

- [ ] **Step 4: Recablear `SesionOperadorPage.tsx`**

En `frontend/src/features/partidas/SesionOperadorPage.tsx`:
1. Añadir import junto a los otros imports de `./` (tras la línea 18 `import { GeoMapPanel, …`):
   ```ts
   import { Countdown } from "./runtimeShared";
   ```
2. Borrar la función local `Countdown` (líneas 397-409, el bloque final del archivo).
3. En `LobbyView`, `<Countdown target={tiempoInicio} />` (línea 310) pasa a:
   ```tsx
   <Countdown target={tiempoInicio} testid="inicio-countdown" expiredLabel="Iniciando…" muted={false} />
   ```
   (`muted={false}` reproduce el `<span>` SIN `className` previo; `expiredLabel` reproduce "Iniciando…".)

- [ ] **Step 5: Correr los tests de runtime, verificar verde sin cambios**

Run:
```bash
cd frontend && npx vitest run src/features/partidas/TriviaRuntimePanel.test.tsx src/features/partidas/BdtRuntimePanel.test.tsx src/features/partidas/SesionOperadorPage.test.tsx
```
Expected: PASS todos. Si algún test falla por markup, la extracción alteró output → corregir el `Countdown` compartido para igualar el original exacto (NO editar los tests).

- [ ] **Step 6: Suite completa + typecheck**

Run: `cd frontend && npx vitest run && npx tsc -b`
Expected: 150/150 (mismo conteo — refactor puro, sin tests nuevos), exit 0. Borrar artifacts de `tsc -b`.

- [ ] **Step 7: Commit**

```bash
cd frontend && git add src/features/partidas/runtimeShared.tsx src/features/partidas/TriviaRuntimePanel.tsx src/features/partidas/BdtRuntimePanel.tsx src/features/partidas/SesionOperadorPage.tsx
git commit -m "refactor(web): runtimeShared Countdown/RankingView compartidos (F-2, bloque 2c4)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 3: `ConsolidadoPanel.tsx` + montar en vista `terminada`

**Files:**
- Create: `frontend/src/features/partidas/ConsolidadoPanel.tsx`
- Create: `frontend/src/features/partidas/ConsolidadoPanel.test.tsx`
- Modify: `frontend/src/features/partidas/SesionOperadorPage.tsx` (vista `terminada`: montar `ConsolidadoPanel`)

**Interfaces:**
- Consumes: `getRankingConsolidado`, `RankingConsolidadoDto`, `PuntuacionesApiError` (Task 1); `formatTiempo` (Task 2).
- Produces: `ConsolidadoPanel({ partidaId, accessToken }: { partidaId: string; accessToken: string }): JSX.Element`.

**Comportamiento:** al montar hace `getRankingConsolidado`. Ante `404`/`409` (lag de proyección: `PartidaFinalizada` llegó pero Puntuaciones aún no materializó) reintenta hasta **3 intentos totales** con **1500 ms** entre cada uno. Éxito con entradas → tabla. `200` con `entradas: []` → "Sin resultados". `404`/`409` persistente o cualquier otro error → mensaje + botón "Reintentar" que reinicia el ciclo.

- [ ] **Step 1: Escribir tests que fallan**

`frontend/src/features/partidas/ConsolidadoPanel.test.tsx`:

```tsx
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ConsolidadoPanel } from "./ConsolidadoPanel";
import * as puntuacionesApi from "../../api/puntuacionesApi";
import { PuntuacionesApiError } from "../../api/puntuacionesApi";

const ranking = {
  partidaId: "p1",
  generadoEn: "2026-07-08T12:00:00Z",
  entradas: [
    {
      posicion: 1,
      competidorId: "abcdef12-0000-0000-0000-000000000000",
      tipoCompetidor: "Participante" as const,
      juegosGanados: 2,
      puntosTotales: 45,
      tiempoTotalMs: 63000
    }
  ]
};

afterEach(() => vi.restoreAllMocks());

describe("ConsolidadoPanel", () => {
  it("muestra la tabla del consolidado al resolver", async () => {
    vi.spyOn(puntuacionesApi, "getRankingConsolidado").mockResolvedValue(ranking);
    render(<ConsolidadoPanel partidaId="p1" accessToken="tok" />);
    const tabla = await screen.findByTestId("ranking-consolidado");
    expect(tabla).toBeInTheDocument();
    expect(screen.getByText("abcdef12")).toBeInTheDocument();
    expect(screen.getByText("45")).toBeInTheDocument();
    expect(screen.getByText("01:03")).toBeInTheDocument(); // 63000ms
  });

  it("200 con entradas vacías muestra 'Sin resultados'", async () => {
    vi.spyOn(puntuacionesApi, "getRankingConsolidado").mockResolvedValue({
      partidaId: "p1",
      generadoEn: "2026-07-08T12:00:00Z",
      entradas: []
    });
    render(<ConsolidadoPanel partidaId="p1" accessToken="tok" />);
    expect(await screen.findByText(/sin resultados/i)).toBeInTheDocument();
  });

  it("reintenta ante 409 y luego muestra la tabla", async () => {
    vi.useFakeTimers();
    const spy = vi
      .spyOn(puntuacionesApi, "getRankingConsolidado")
      .mockRejectedValueOnce(new PuntuacionesApiError("no terminada", 409))
      .mockResolvedValueOnce(ranking);
    render(<ConsolidadoPanel partidaId="p1" accessToken="tok" />);
    await vi.advanceTimersByTimeAsync(1600); // dispara el 2º intento
    vi.useRealTimers();
    expect(await screen.findByTestId("ranking-consolidado")).toBeInTheDocument();
    expect(spy).toHaveBeenCalledTimes(2);
  });

  it("409 persistente muestra aviso y botón Reintentar que vuelve a pedir", async () => {
    vi.useFakeTimers();
    const spy = vi
      .spyOn(puntuacionesApi, "getRankingConsolidado")
      .mockRejectedValue(new PuntuacionesApiError("no terminada", 409));
    render(<ConsolidadoPanel partidaId="p1" accessToken="tok" />);
    await vi.advanceTimersByTimeAsync(3200); // agota los 3 intentos (2 esperas de 1500)
    vi.useRealTimers();
    expect(await screen.findByText(/no disponible aún/i)).toBeInTheDocument();
    expect(spy).toHaveBeenCalledTimes(3);

    spy.mockResolvedValueOnce(ranking);
    await userEvent.click(screen.getByRole("button", { name: /reintentar/i }));
    expect(await screen.findByTestId("ranking-consolidado")).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Correr tests, verificar que fallan**

Run: `cd frontend && npx vitest run src/features/partidas/ConsolidadoPanel.test.tsx`
Expected: FAIL (módulo no existe).

- [ ] **Step 3: Implementar `ConsolidadoPanel.tsx`**

```tsx
// Ranking consolidado de la partida terminada (RF-45). GET puntual a Puntuaciones con
// reintento corto ante 404/409 (lag de proyección tras PartidaFinalizada).
import { useEffect, useState } from "react";
import {
  getRankingConsolidado,
  PuntuacionesApiError,
  type RankingConsolidadoDto
} from "../../api/puntuacionesApi";
import { formatTiempo } from "./runtimeShared";

const MAX_INTENTOS = 3;
const ESPERA_MS = 1500;

type Estado =
  | { status: "cargando" }
  | { status: "ok"; ranking: RankingConsolidadoDto }
  | { status: "no-disponible" };

export function ConsolidadoPanel({
  partidaId,
  accessToken
}: {
  partidaId: string;
  accessToken: string;
}) {
  const [estado, setEstado] = useState<Estado>({ status: "cargando" });
  const [intentoManual, setIntentoManual] = useState(0);

  useEffect(() => {
    let active = true;
    let timer: ReturnType<typeof setTimeout> | undefined;
    let intentos = 0;

    const cargar = () => {
      getRankingConsolidado(partidaId, accessToken)
        .then((ranking) => {
          if (active) setEstado({ status: "ok", ranking });
        })
        .catch((caught) => {
          if (!active) return;
          const code = caught instanceof PuntuacionesApiError ? caught.statusCode : 0;
          intentos += 1;
          if ((code === 404 || code === 409) && intentos < MAX_INTENTOS) {
            timer = setTimeout(cargar, ESPERA_MS);
            return;
          }
          setEstado({ status: "no-disponible" });
        });
    };

    setEstado({ status: "cargando" });
    cargar();
    return () => {
      active = false;
      if (timer) clearTimeout(timer);
    };
  }, [partidaId, accessToken, intentoManual]);

  return (
    <div className="card stack" data-testid="consolidado-panel">
      <h1>Partida finalizada</h1>
      {estado.status === "cargando" ? <p className="muted">Cargando consolidado…</p> : null}
      {estado.status === "no-disponible" ? (
        <div className="stack">
          <p className="muted">Consolidado no disponible aún.</p>
          <button type="button" onClick={() => setIntentoManual((n) => n + 1)}>
            Reintentar
          </button>
        </div>
      ) : null}
      {estado.status === "ok" ? <ConsolidadoTabla ranking={estado.ranking} /> : null}
    </div>
  );
}

function ConsolidadoTabla({ ranking }: { ranking: RankingConsolidadoDto }) {
  if (!ranking.entradas.length) {
    return <p className="muted">Sin resultados.</p>;
  }
  return (
    <div className="table-wrap">
      <table aria-label="Ranking consolidado" data-testid="ranking-consolidado">
        <thead>
          <tr>
            <th scope="col">Posición</th>
            <th scope="col">Competidor</th>
            <th scope="col">Juegos ganados</th>
            <th scope="col">Puntos totales</th>
            <th scope="col">Tiempo total</th>
          </tr>
        </thead>
        <tbody>
          {ranking.entradas.map((entrada) => (
            <tr key={entrada.competidorId}>
              <td>{entrada.posicion}</td>
              <td>{entrada.competidorId.slice(0, 8)}</td>
              <td>{entrada.juegosGanados}</td>
              <td>{entrada.puntosTotales}</td>
              <td>{formatTiempo(entrada.tiempoTotalMs)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
```

- [ ] **Step 4: Montar en la vista `terminada` de `SesionOperadorPage.tsx`**

1. Añadir import junto a los paneles (tras `import { Countdown } from "./runtimeShared";` de Task 2):
   ```ts
   import { ConsolidadoPanel } from "./ConsolidadoPanel";
   ```
2. Reemplazar el `case "terminada":` de `renderVista` (actualmente líneas 233-238, el `<div className="card stack"><p>La partida finalizó.</p></div>`) por:
   ```tsx
   case "terminada":
     return <ConsolidadoPanel partidaId={ctx.partidaId} accessToken={ctx.accessToken} />;
   ```

- [ ] **Step 5: Correr tests del panel + de la página**

Run:
```bash
cd frontend && npx vitest run src/features/partidas/ConsolidadoPanel.test.tsx src/features/partidas/SesionOperadorPage.test.tsx
```
Expected: PASS. Si `SesionOperadorPage.test.tsx` asertaba el texto "La partida finalizó", actualizar esa aserción a la nueva vista (mock `getRankingConsolidado` o assert de `consolidado-panel`). **Esta es la única edición de test permitida en esta tarea** (el `<p>` viejo dejó de existir).

- [ ] **Step 6: Suite completa + typecheck**

Run: `cd frontend && npx vitest run && npx tsc -b`
Expected: 154/154 (+4 de ConsolidadoPanel), exit 0. Borrar artifacts.

- [ ] **Step 7: Commit**

```bash
cd frontend && git add src/features/partidas/ConsolidadoPanel.tsx src/features/partidas/ConsolidadoPanel.test.tsx src/features/partidas/SesionOperadorPage.tsx
git commit -m "feat(web): ConsolidadoPanel en vista terminada con reintento (bloque 2c4)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 4: Retiro de páginas/APIs legacy

**Files:**
- Delete: `frontend/src/features/trivia/TriviaOperationsPage.tsx`, `frontend/src/features/trivia/TriviaOperationsPage.test.tsx`
- Delete: `frontend/src/features/bdt/PublishedBdtGamesPage.tsx`, `frontend/src/features/bdt/PublishedBdtGamesPage.test.tsx`
- Delete: `frontend/src/api/triviaApi.ts`, `frontend/src/api/bdtApi.ts`, `frontend/src/api/bdtApi.test.ts`
- Modify: `frontend/src/app/App.tsx` (quitar imports + rutas `trivia/operar` y `bdt/partidas`)
- Modify: `frontend/src/shell/navConfig.tsx` (quitar áreas `trivia` y `bdt` + imports huérfanos `Play`, `MapPin`)
- Modify: `frontend/src/app/App.test.tsx` (quitar import de `bdtApi` + el test de navegación BDT)

**Contexto:** confirmado por grep que estos archivos son autocontenidos — solo se importan entre sí y desde `App.tsx`/`App.test.tsx`/`navConfig.tsx`. La consola nueva (`/partidas/:id/sesion`) los reemplaza. `Flag` y `ListChecks` SIGUEN usados por el área `partidas`; NO borrarlos. Solo `Play` (solo área trivia) y `MapPin` (solo área bdt) quedan huérfanos.

- [ ] **Step 1: Borrar los archivos legacy**

```bash
cd frontend && git rm \
  src/features/trivia/TriviaOperationsPage.tsx \
  src/features/trivia/TriviaOperationsPage.test.tsx \
  src/features/bdt/PublishedBdtGamesPage.tsx \
  src/features/bdt/PublishedBdtGamesPage.test.tsx \
  src/api/triviaApi.ts \
  src/api/bdtApi.ts \
  src/api/bdtApi.test.ts
```

- [ ] **Step 2: Limpiar `App.tsx`**

En `frontend/src/app/App.tsx`:
1. Borrar la línea 13 `import { PublishedBdtGamesPage } from "../features/bdt/PublishedBdtGamesPage";`
2. Borrar la línea 21 `import { TriviaOperationsPage } from "../features/trivia/TriviaOperationsPage";`
3. Borrar el bloque de ruta `trivia/operar` completo (objeto `{ path: "trivia/operar", element: (…) }`, líneas 151-158) incluyendo su coma.
4. Borrar el bloque de ruta `bdt/partidas` completo (líneas 159-166) incluyendo su coma. La ruta `{ path: "*", element: <NotFoundScreen /> }` queda como último hijo.

- [ ] **Step 3: Limpiar `navConfig.tsx`**

En `frontend/src/shell/navConfig.tsx`:
1. Borrar el objeto de área `trivia` completo (líneas 43-49, `{ id: "trivia", … }`) incluyendo su coma.
2. Borrar el objeto de área `bdt` completo (líneas 50-56, `{ id: "bdt", … }`).
3. Ajustar el import de la línea 1 quitando `Play` y `MapPin` (quedan usados: `Flag, IconComponent, ListChecks, Lock, Plus, UserPlus, Users`):
   ```ts
   import { Flag, IconComponent, ListChecks, Lock, Plus, UserPlus, Users } from "./icons";
   ```

- [ ] **Step 4: Limpiar `App.test.tsx`**

En `frontend/src/app/App.test.tsx`:
1. Borrar la línea 4 `import * as bdtApi from "../api/bdtApi";`
2. Borrar el test completo `it("navigates an operator to published BDT games", …)` (líneas 63-79).

- [ ] **Step 5: Grep de limpieza — sin referencias colgantes**

Run:
```bash
cd frontend && grep -rn "triviaApi\|bdtApi\|TriviaOperationsPage\|PublishedBdtGamesPage\|trivia/operar\|bdt/partidas" src
```
Expected: sin resultados (exit 1 / vacío). Si algo aparece, es una referencia colgante → resolver.

- [ ] **Step 6: Suite completa + typecheck**

Run: `cd frontend && npx vitest run && npx tsc -b`
Expected: verde (conteo baja por los tests borrados: TriviaOperationsPage + PublishedBdtGamesPage + bdtApi + el test BDT de App = varios menos; el número exacto lo reporta vitest). `tsc -b` exit 0 (sin imports rotos). Borrar artifacts.

- [ ] **Step 7: Commit**

```bash
cd frontend && git add -A src/app/App.tsx src/app/App.test.tsx src/shell/navConfig.tsx
git commit -m "chore(web): retiro paginas/apis legacy trivia+bdt (bloque 2c4)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

(`git rm` de Step 1 ya stageó los borrados; `git add -A` sobre los 3 modificados completa el commit.)

---

### Task 5: Gate final — build, suite, traceability y E2E vivo (controller)

**Files:**
- Modify: `docs/04-sdd/traceability-matrix.md` (fila 2c-4 + nota "Bloque 2c COMPLETO")
- Modify: `GUIA-LEVANTAMIENTO.md` (si aplica: nota consolidado en la sección de la consola)

Esta tarea la ejecuta el controller (el build valida leaflet — F-1 lado build; el E2E requiere el stack vivo que levanta el usuario).

- [ ] **Step 1: Build de producción (cubre F-1 leaflet)**

Run: `cd frontend && npm run build`
Expected: `tsc -b` + `vite build` exit 0. El bundle incluye leaflet + su CSS sin error de resolución (verificación build-side de F-1). El pintado real en navegador es chequeo manual opcional (react-leaflet@4 es React-18-StrictMode-safe).

- [ ] **Step 2: Suite completa verde**

Run: `cd frontend && npx vitest run`
Expected: verde (baseline 150 + 3 ConsolidadoPanel + 3 puntuacionesApi − tests legacy borrados; conteo neto que reporte vitest).

- [ ] **Step 3: E2E consolidado con stack vivo**

Con el stack levantado por el usuario (infra compose + partidas + operaciones-sesion + puntuaciones + gateway, tokens PKCE operador+participante), vía gateway `:5080`:
1. Crear partida con ≥1 juego, publicar, inscribir participante, iniciar.
2. Antes de finalizar: `GET /puntuaciones/partidas/{id}/ranking-consolidado` → **409** (partida no Terminada).
3. Jugar hasta marcar puntaje, finalizar el último juego (`terminada: true`).
4. `GET /puntuaciones/partidas/{id}/ranking-consolidado` → **200** con `entradas` (juegosGanados/puntosTotales/tiempoTotalMs).

Registrar el resultado en el ledger.

- [ ] **Step 4: Traceability + GUIA**

Añadir fila 2c-4 a `docs/04-sdd/traceability-matrix.md` con los hashes de T1-T4 (verificar cada uno con `git cat-file -t <hash>`) y la nota de cierre "Bloque 2c COMPLETO". Actualizar `GUIA-LEVANTAMIENTO.md` si la consola necesita nota del consolidado.

- [ ] **Step 5: Commit docs**

```bash
git add docs/04-sdd/traceability-matrix.md GUIA-LEVANTAMIENTO.md
git commit -m "docs(bloque2c4): traceability consolidado + cierre bloque 2c" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Self-Review

**Spec coverage:**
- §1 `getRankingConsolidado` → Task 1. ✅
- §2 F-2 `runtimeShared` → Task 2. ✅
- §3 `ConsolidadoPanel` + terminada + reintento → Task 3. ✅
- §4 retiro legacy → Task 4. ✅
- §5 gate (build/test/E2E/traceability) → Task 5. ✅

**Placeholder scan:** sin TBD/TODO; todo el código va literal. ✅

**Type consistency:** `RankingConsolidadoDto`/`RankingConsolidadoEntradaDto` definidos en Task 1, consumidos en Task 3 con los mismos nombres de campo (`juegosGanados`, `puntosTotales`, `tiempoTotalMs`). `formatTiempo` exportado en Task 2, consumido en Task 3. `Countdown`/`RankingView` exportados en Task 2 con las firmas que Tasks-consumidores usan. `PuntuacionesApiError` reusado (ya existía). ✅

**Testids:** `pregunta-countdown`/`etapa-countdown`/`inicio-countdown`/`ranking-juego` preservados exactos vía props del `Countdown` compartido; nuevos `consolidado-panel`/`ranking-consolidado`. ✅
