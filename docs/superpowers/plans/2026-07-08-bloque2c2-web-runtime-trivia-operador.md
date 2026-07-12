# Bloque 2c-2 — Runtime Trivia operador (web) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Llenar el runtime Trivia de la consola de sesión del operador: pregunta activa con countdown y correcta marcada, avance manual, finalización del juego y ranking del juego en vivo.

**Architecture:** Consumo web puro contra el gateway `:5080`. Se extiende `operacionesApi` (3 funciones) y `useSesionHub` (2 eventos), se crea `puntuacionesApi` (1 GET) y un componente `TriviaRuntimePanel` con frontera limpia (la página le pasa `refetchSignal` numérico; el hub queda en la página). La página paga los arrastres de 2c-1: F1 seq-guard en `cargar()` y F5 pills por estado de juego. Sin segundo hub SignalR: en Trivia los puntos cambian solo al cerrar pregunta → GET de ranking disparado por `PreguntaActivada`/`PreguntaCerrada`.

**Tech Stack:** React 18 + Vite + TypeScript, react-router-dom v6, `@microsoft/signalr` (ya instalada), vitest + @testing-library/react + user-event.

## Global Constraints

- Todo el tráfico backend vía gateway; base URL desde `import.meta.env.VITE_GATEWAY_BASE_URL` (patrón `resolveBaseUrl` idéntico a `partidasApi.ts`/`operacionesApi.ts`).
- **Cero dependencias nuevas.**
- Enums como string; JSON camelCase.
- `409` en `GET /pregunta-actual` = estado válido "sin pregunta activa", nunca error fatal. `404` en ranking (proyección best-effort ADR-0012) = "sin datos todavía", nunca error fatal.
- No romper testids/labels/aria existentes. testids nuevos definidos aquí son la fuente: `trivia-runtime`, `pregunta-activa`, `sin-pregunta-activa`, `btn-avanzar-pregunta`, `btn-finalizar-juego`, `ranking-juego`, `opcion-correcta`, `pregunta-countdown`, `runtime-bdt-placeholder`.
- Typecheck gate: **`npx tsc -b`** desde `frontend/` (salida vacía = limpio; borrar los artifacts untracked que deja: `*.tsbuildinfo`, `vite.config.js/.d.ts`, `vitest.config.js/.d.ts`). NUNCA `tsc --noEmit` pelado (no-op en este repo).
- Commits terminan con trailer exacto: `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- Subagentes: solo `git add <ruta>` archivo por archivo. PROHIBIDO `git stash/reset/checkout/restore/clean`.
- Comandos desde `frontend/`: `npm test` (vitest run) verde antes de cada commit.

---

### Task 1: Extender `operacionesApi` (pregunta actual, avance, finalizar juego)

**Files:**
- Modify: `frontend/src/api/operacionesApi.ts`
- Modify: `frontend/src/api/operacionesApi.test.ts`

**Interfaces:**
- Consumes: `request<T>`/`buildAuthHeaders`/`OperacionesApiError` ya existentes en el archivo.
- Produces (consumidos por Task 4):
  - `interface OpcionPregunta { opcionId: string; texto: string }`
  - `interface PreguntaActualDto { partidaId: string; juegoId: string; preguntaId: string; orden: number; texto: string; tiempoLimiteSegundos: number; fechaActivacion: string; opciones: OpcionPregunta[] }`
  - `interface AvancePreguntaResponse { partidaId: string; preguntaCerradaOrden: number; preguntaActivadaOrden?: number | null; sinMasPreguntas: boolean }`
  - `interface AvanceJuegoResponse { partidaId: string; estado: string; juegoFinalizadoOrden?: number | null; juegoActivadoOrden?: number | null; terminada: boolean }`
  - `getPreguntaActual(partidaId, accessToken, fetchImpl?): Promise<PreguntaActualDto>`
  - `avanzarPregunta(partidaId, accessToken, fetchImpl?): Promise<AvancePreguntaResponse>`
  - `finalizarJuegoActual(partidaId, accessToken, fetchImpl?): Promise<AvanceJuegoResponse>`

- [ ] **Step 1: Write the failing tests**

Añadir al final del `describe("operacionesApi", ...)` en `frontend/src/api/operacionesApi.test.ts` (imports nuevos: `avanzarPregunta`, `finalizarJuegoActual`, `getPreguntaActual`):

```ts
  it("getPreguntaActual hace GET a pregunta-actual y un 409 lanza error con statusCode", async () => {
    const ok = okJson({
      partidaId: "p1",
      juegoId: "j1",
      preguntaId: "q1",
      orden: 1,
      texto: "2+2?",
      tiempoLimiteSegundos: 30,
      fechaActivacion: "2026-07-08T12:00:00Z",
      opciones: [{ opcionId: "o1", texto: "4" }]
    });
    const r = await getPreguntaActual("p1", "tok", ok);
    expect(r.opciones[0].opcionId).toBe("o1");
    expect(ok.mock.calls[0][0]).toBe(
      "https://gw.example.test/operaciones-sesion/partidas/p1/pregunta-actual"
    );
    expect(ok.mock.calls[0][1].method).toBe("GET");

    const sin = okJson({ message: "sin pregunta activa" }, 409);
    await expect(getPreguntaActual("p1", "tok", sin)).rejects.toMatchObject({ statusCode: 409 });
  });

  it("avanzarPregunta hace POST al avance y devuelve sinMasPreguntas", async () => {
    const f = okJson({ partidaId: "p1", preguntaCerradaOrden: 2, preguntaActivadaOrden: null, sinMasPreguntas: true });
    const r = await avanzarPregunta("p1", "tok", f);
    expect(r.sinMasPreguntas).toBe(true);
    expect(f.mock.calls[0][0]).toBe(
      "https://gw.example.test/operaciones-sesion/partidas/p1/pregunta-actual/avance"
    );
    expect(f.mock.calls[0][1].method).toBe("POST");
  });

  it("finalizarJuegoActual hace POST a la finalizacion y devuelve terminada", async () => {
    const f = okJson({ partidaId: "p1", estado: "Terminada", juegoFinalizadoOrden: 1, juegoActivadoOrden: null, terminada: true });
    const r = await finalizarJuegoActual("p1", "tok", f);
    expect(r.terminada).toBe(true);
    expect(f.mock.calls[0][0]).toBe(
      "https://gw.example.test/operaciones-sesion/partidas/p1/juego-actual/finalizacion"
    );
    expect(f.mock.calls[0][1].method).toBe("POST");
  });
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd frontend && npx vitest run src/api/operacionesApi.test.ts`
Expected: FAIL — `getPreguntaActual` etc. no exportados.

- [ ] **Step 3: Write minimal implementation**

Añadir a `frontend/src/api/operacionesApi.ts`, tipos junto a los demás tipos y funciones al final (mismo estilo):

```ts
export interface OpcionPregunta {
  opcionId: string;
  texto: string;
}

export interface PreguntaActualDto {
  partidaId: string;
  juegoId: string;
  preguntaId: string;
  orden: number;
  texto: string;
  tiempoLimiteSegundos: number;
  fechaActivacion: string;
  opciones: OpcionPregunta[];
}

export interface AvancePreguntaResponse {
  partidaId: string;
  preguntaCerradaOrden: number;
  preguntaActivadaOrden?: number | null;
  sinMasPreguntas: boolean;
}

export interface AvanceJuegoResponse {
  partidaId: string;
  estado: string;
  juegoFinalizadoOrden?: number | null;
  juegoActivadoOrden?: number | null;
  terminada: boolean;
}

export async function getPreguntaActual(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<PreguntaActualDto> {
  return request<PreguntaActualDto>(
    `/operaciones-sesion/partidas/${partidaId}/pregunta-actual`,
    { method: "GET", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}

export async function avanzarPregunta(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<AvancePreguntaResponse> {
  return request<AvancePreguntaResponse>(
    `/operaciones-sesion/partidas/${partidaId}/pregunta-actual/avance`,
    { method: "POST", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}

export async function finalizarJuegoActual(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<AvanceJuegoResponse> {
  return request<AvanceJuegoResponse>(
    `/operaciones-sesion/partidas/${partidaId}/juego-actual/finalizacion`,
    { method: "POST", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}
```

- [ ] **Step 4: Run tests + typecheck**

Run: `cd frontend && npx vitest run src/api/operacionesApi.test.ts && npx tsc -b`
Expected: PASS (8 tests: 5 previos + 3 nuevos) + `tsc -b` sin salida. Borrar artifacts de `tsc -b` (`rm -f tsconfig.*.tsbuildinfo vite.config.js vite.config.d.ts vitest.config.js vitest.config.d.ts` en `frontend/`).

- [ ] **Step 5: Commit**

```bash
git add frontend/src/api/operacionesApi.ts frontend/src/api/operacionesApi.test.ts
git commit -m "$(printf 'feat(web): operacionesApi runtime trivia — pregunta, avance, finalizar (bloque 2c2)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>')"
```

---

### Task 2: Cliente `puntuacionesApi`

**Files:**
- Create: `frontend/src/api/puntuacionesApi.ts`
- Test: `frontend/src/api/puntuacionesApi.test.ts`

**Interfaces:**
- Consumes: nada (módulo nuevo, patrón duplicado a propósito — es el idiom del repo).
- Produces (consumidos por Task 4):
  - `class PuntuacionesApiError extends Error { statusCode: number }`
  - `interface RankingEntrada { posicion: number; competidorId: string; tipoCompetidor: "Participante" | "Equipo"; puntos: number; tiempoAcumuladoMs: number; unidadesGanadas: number }`
  - `interface RankingJuegoDto { juegoId: string; tipoJuego: string; generadoEn: string; entradas: RankingEntrada[] }`
  - `getRankingJuego(partidaId, juegoId, accessToken, fetchImpl?): Promise<RankingJuegoDto>`

- [ ] **Step 1: Write the failing test**

`frontend/src/api/puntuacionesApi.test.ts`:

```ts
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { getRankingJuego, PuntuacionesApiError } from "./puntuacionesApi";

const okJson = (body: unknown, status = 200) =>
  vi.fn().mockResolvedValue(new Response(JSON.stringify(body), { status }));

describe("puntuacionesApi", () => {
  beforeEach(() => vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test/"));
  afterEach(() => vi.unstubAllEnvs());

  it("getRankingJuego hace GET autenticado al ranking del juego", async () => {
    const f = okJson({
      juegoId: "j1",
      tipoJuego: "Trivia",
      generadoEn: "2026-07-08T12:00:00Z",
      entradas: [
        {
          posicion: 1,
          competidorId: "c1",
          tipoCompetidor: "Participante",
          puntos: 30,
          tiempoAcumuladoMs: 12345,
          unidadesGanadas: 3
        }
      ]
    });
    const r = await getRankingJuego("p1", "j1", "tok", f);
    expect(r.entradas[0].puntos).toBe(30);
    expect(f.mock.calls[0][0]).toBe(
      "https://gw.example.test/puntuaciones/partidas/p1/juegos/j1/ranking"
    );
    expect((f.mock.calls[0][1].headers as Record<string, string>).Authorization).toBe("Bearer tok");
  });

  it("404 de proyeccion lanza PuntuacionesApiError con statusCode", async () => {
    const f = okJson({ message: "no proyectado" }, 404);
    await expect(getRankingJuego("p1", "j1", "tok", f)).rejects.toMatchObject({ statusCode: 404 });
    await expect(getRankingJuego("p1", "j1", "tok", f)).rejects.toBeInstanceOf(PuntuacionesApiError);
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd frontend && npx vitest run src/api/puntuacionesApi.test.ts`
Expected: FAIL — no existe `./puntuacionesApi`.

- [ ] **Step 3: Write minimal implementation**

`frontend/src/api/puntuacionesApi.ts`:

```ts
// Cliente HTTP del servicio Puntuaciones (rankings, lectura), via gateway.
export interface RankingEntrada {
  posicion: number;
  competidorId: string;
  tipoCompetidor: "Participante" | "Equipo";
  puntos: number;
  tiempoAcumuladoMs: number;
  unidadesGanadas: number;
}

export interface RankingJuegoDto {
  juegoId: string;
  tipoJuego: string;
  generadoEn: string;
  entradas: RankingEntrada[];
}

export class PuntuacionesApiError extends Error {
  constructor(
    message: string,
    public readonly statusCode: number
  ) {
    super(message);
    this.name = "PuntuacionesApiError";
  }
}

function resolveBaseUrl(): string {
  const value = import.meta.env.VITE_GATEWAY_BASE_URL as string | undefined;
  if (!value) {
    throw new Error("Missing VITE_GATEWAY_BASE_URL environment variable.");
  }
  return value.replace(/\/$/, "");
}

export async function getRankingJuego(
  partidaId: string,
  juegoId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<RankingJuegoDto> {
  const response = await fetchImpl(
    `${resolveBaseUrl()}/puntuaciones/partidas/${partidaId}/juegos/${juegoId}/ranking`,
    { method: "GET", headers: { Authorization: `Bearer ${accessToken}` } }
  );
  const body = (await response.json().catch(() => ({}))) as RankingJuegoDto & { message?: string };
  if (!response.ok) {
    const message = body.message ?? `Puntuaciones API error. Status=${response.status}`;
    throw new PuntuacionesApiError(message, response.status);
  }
  return body;
}
```

- [ ] **Step 4: Run test + typecheck**

Run: `cd frontend && npx vitest run src/api/puntuacionesApi.test.ts && npx tsc -b`
Expected: PASS (2 tests) + tsc limpio. Borrar artifacts de `tsc -b`.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/api/puntuacionesApi.ts frontend/src/api/puntuacionesApi.test.ts
git commit -m "$(printf 'feat(web): cliente puntuacionesApi ranking de juego (bloque 2c2)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>')"
```

---

### Task 3: Extender `useSesionHub` (PreguntaActivada / PreguntaCerrada)

**Files:**
- Modify: `frontend/src/features/partidas/useSesionHub.ts`
- Modify: `frontend/src/features/partidas/useSesionHub.test.ts`

**Interfaces:**
- Produces (consumidos por Task 5):
  - `SesionHubHandlers` gana `onPreguntaActivada?: (p: { partidaId: string; juegoId: string; preguntaId: string; orden: number; fechaLimiteUtc: string }) => void` y `onPreguntaCerrada?: (p: { partidaId: string; juegoId: string; preguntaId: string }) => void`.

- [ ] **Step 1: Write the failing test**

Añadir al `describe("useSesionHub", ...)`:

```ts
  it("rutea PreguntaActivada y PreguntaCerrada a sus handlers", async () => {
    const conn = fakeConnection();
    vi.mocked(crearSesionHub).mockReturnValue(conn as never);
    const onPreguntaActivada = vi.fn();
    const onPreguntaCerrada = vi.fn();

    renderHook(() => useSesionHub("p1", "tok", { onPreguntaActivada, onPreguntaCerrada }));
    await Promise.resolve();

    conn.handlers["PreguntaActivada"]({ partidaId: "p1", juegoId: "j1", preguntaId: "q1", orden: 1, fechaLimiteUtc: "2026-07-08T12:00:30Z" });
    expect(onPreguntaActivada).toHaveBeenCalledWith(
      expect.objectContaining({ preguntaId: "q1", fechaLimiteUtc: "2026-07-08T12:00:30Z" })
    );

    conn.handlers["PreguntaCerrada"]({ partidaId: "p1", juegoId: "j1", preguntaId: "q1" });
    expect(onPreguntaCerrada).toHaveBeenCalledWith(expect.objectContaining({ preguntaId: "q1" }));
  });
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd frontend && npx vitest run src/features/partidas/useSesionHub.test.ts`
Expected: FAIL — `conn.handlers["PreguntaActivada"]` undefined (evento no registrado).

- [ ] **Step 3: Write minimal implementation**

En `useSesionHub.ts`, añadir a `SesionHubHandlers`:

```ts
  onPreguntaActivada?: (payload: {
    partidaId: string;
    juegoId: string;
    preguntaId: string;
    orden: number;
    fechaLimiteUtc: string;
  }) => void;
  onPreguntaCerrada?: (payload: { partidaId: string; juegoId: string; preguntaId: string }) => void;
```

Y junto a los otros `connection.on(...)` (antes de `onreconnected`):

```ts
    connection.on("PreguntaActivada", (p) => handlersRef.current.onPreguntaActivada?.(p));
    connection.on("PreguntaCerrada", (p) => handlersRef.current.onPreguntaCerrada?.(p));
```

- [ ] **Step 4: Run test + typecheck**

Run: `cd frontend && npx vitest run src/features/partidas/useSesionHub.test.ts && npx tsc -b`
Expected: PASS (3 tests) + tsc limpio. Borrar artifacts.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/partidas/useSesionHub.ts frontend/src/features/partidas/useSesionHub.test.ts
git commit -m "$(printf 'feat(web): useSesionHub rutea PreguntaActivada/PreguntaCerrada (bloque 2c2)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>')"
```

---

### Task 4: Componente `TriviaRuntimePanel`

**Files:**
- Create: `frontend/src/features/partidas/TriviaRuntimePanel.tsx`
- Test: `frontend/src/features/partidas/TriviaRuntimePanel.test.tsx`

**Interfaces:**
- Consumes: `getPreguntaActual`, `avanzarPregunta`, `finalizarJuegoActual`, `OperacionesApiError`, tipos (Task 1); `getRankingJuego`, `PuntuacionesApiError`, `RankingJuegoDto` (Task 2); `PreguntaDetail` de `partidasApi` (2b: `{ preguntaId, texto, puntajeAsignado, tiempoLimiteSegundos, opciones[{opcionId, texto, esCorrecta}] }`).
- Produces (consumido por Task 5):
  - `export function TriviaRuntimePanel(props: TriviaRuntimePanelProps)`
  - `interface TriviaRuntimePanelProps { partidaId: string; juegoId: string; accessToken: string; preguntasConfig: PreguntaDetail[]; refetchSignal: number; onTerminada: () => void; onJuegoAvanzado: () => void }`

**Lógica obligatoria:**
- Efecto con deps `[partidaId, juegoId, accessToken, refetchSignal]`: fetch de pregunta y ranking en paralelo. Pregunta: `200` → estado `activa`; `OperacionesApiError` con `statusCode === 409` → estado `sin-pregunta`; otro error → estado `error`. Ranking: `200` → data; error (404 u otro) → `null` (leyenda "Sin datos de ranking todavía").
- Cruce de correcta: buscar la pregunta de config por `preguntaId`; si no hay match, por `texto`. Dentro, opción correcta por `opcionId`; fallback por `texto`. Sin match → sin badge (nunca romper).
- Countdown: reusar el patrón `Countdown` local (target = `new Date(new Date(pregunta.fechaActivacion).getTime() + pregunta.tiempoLimiteSegundos * 1000).toISOString()` — igual a `fechaLimiteUtc` del contrato).
- `onAvanzar` (`btn-avanzar-pregunta`, disabled mientras postea): `avanzarPregunta` → refetch interno (mismo efecto: usar un contador interno o repetir el fetch); `catch` → refetch interno (409 de carrera es normal).
- `onFinalizar` (`btn-finalizar-juego`, disabled mientras postea): `finalizarJuegoActual` → si `terminada` → `props.onTerminada()`; si `juegoActivadoOrden != null` → `props.onJuegoAvanzado()`; `catch` → refetch interno.
- Tiempo del ranking en `mm:ss` desde `tiempoAcumuladoMs`; `competidorId` mostrado como primeros 8 chars.

- [ ] **Step 1: Write the failing test**

`frontend/src/features/partidas/TriviaRuntimePanel.test.tsx`:

```tsx
import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { TriviaRuntimePanel } from "./TriviaRuntimePanel";
import {
  avanzarPregunta,
  finalizarJuegoActual,
  getPreguntaActual,
  OperacionesApiError,
  type PreguntaActualDto
} from "../../api/operacionesApi";
import { getRankingJuego, type RankingJuegoDto } from "../../api/puntuacionesApi";
import type { PreguntaDetail } from "../../api/partidasApi";

vi.mock("../../api/operacionesApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../api/operacionesApi")>();
  return { ...actual, getPreguntaActual: vi.fn(), avanzarPregunta: vi.fn(), finalizarJuegoActual: vi.fn() };
});
vi.mock("../../api/puntuacionesApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../api/puntuacionesApi")>();
  return { ...actual, getRankingJuego: vi.fn() };
});

const pregunta: PreguntaActualDto = {
  partidaId: "p1",
  juegoId: "j1",
  preguntaId: "q1",
  orden: 1,
  texto: "2+2?",
  tiempoLimiteSegundos: 30,
  fechaActivacion: new Date().toISOString(),
  opciones: [
    { opcionId: "o1", texto: "4" },
    { opcionId: "o2", texto: "5" }
  ]
};
const config: PreguntaDetail[] = [
  {
    preguntaId: "q1",
    texto: "2+2?",
    puntajeAsignado: 10,
    tiempoLimiteSegundos: 30,
    opciones: [
      { opcionId: "o1", texto: "4", esCorrecta: true },
      { opcionId: "o2", texto: "5", esCorrecta: false }
    ]
  }
];
const ranking: RankingJuegoDto = {
  juegoId: "j1",
  tipoJuego: "Trivia",
  generadoEn: "2026-07-08T12:00:00Z",
  entradas: [
    { posicion: 1, competidorId: "abcdef12-0000-0000-0000-000000000000", tipoCompetidor: "Participante", puntos: 10, tiempoAcumuladoMs: 61000, unidadesGanadas: 1 }
  ]
};

function renderPanel(props: Partial<Parameters<typeof TriviaRuntimePanel>[0]> = {}) {
  return render(
    <TriviaRuntimePanel
      partidaId="p1"
      juegoId="j1"
      accessToken="tok"
      preguntasConfig={config}
      refetchSignal={0}
      onTerminada={vi.fn()}
      onJuegoAvanzado={vi.fn()}
      {...props}
    />
  );
}

describe("TriviaRuntimePanel", () => {
  beforeEach(() => {
    vi.mocked(getPreguntaActual).mockReset();
    vi.mocked(avanzarPregunta).mockReset();
    vi.mocked(finalizarJuegoActual).mockReset();
    vi.mocked(getRankingJuego).mockReset();
  });

  it("con pregunta activa muestra texto, opciones, correcta marcada, countdown y ranking", async () => {
    vi.mocked(getPreguntaActual).mockResolvedValue(pregunta);
    vi.mocked(getRankingJuego).mockResolvedValue(ranking);
    renderPanel();

    expect(await screen.findByTestId("pregunta-activa")).toBeInTheDocument();
    expect(screen.getByText("2+2?")).toBeInTheDocument();
    expect(screen.getByTestId("opcion-correcta")).toHaveTextContent("4");
    expect(screen.getByTestId("pregunta-countdown")).toBeInTheDocument();
    const tabla = screen.getByTestId("ranking-juego");
    expect(tabla).toHaveTextContent("abcdef12");
    expect(tabla).toHaveTextContent("10");
    expect(tabla).toHaveTextContent("01:01");
  });

  it("con 409 muestra sin-pregunta-activa y Finalizar juego; terminada llama onTerminada", async () => {
    vi.mocked(getPreguntaActual).mockRejectedValue(new OperacionesApiError("sin pregunta", 409));
    vi.mocked(getRankingJuego).mockResolvedValue(ranking);
    vi.mocked(finalizarJuegoActual).mockResolvedValue({
      partidaId: "p1", estado: "Terminada", juegoFinalizadoOrden: 1, juegoActivadoOrden: null, terminada: true
    });
    const onTerminada = vi.fn();
    renderPanel({ onTerminada });

    expect(await screen.findByTestId("sin-pregunta-activa")).toBeInTheDocument();
    await userEvent.click(screen.getByTestId("btn-finalizar-juego"));
    expect(onTerminada).toHaveBeenCalled();
  });

  it("finalizar que activa el siguiente juego llama onJuegoAvanzado", async () => {
    vi.mocked(getPreguntaActual).mockRejectedValue(new OperacionesApiError("sin pregunta", 409));
    vi.mocked(getRankingJuego).mockResolvedValue(ranking);
    vi.mocked(finalizarJuegoActual).mockResolvedValue({
      partidaId: "p1", estado: "Iniciada", juegoFinalizadoOrden: 1, juegoActivadoOrden: 2, terminada: false
    });
    const onJuegoAvanzado = vi.fn();
    renderPanel({ onJuegoAvanzado });

    await userEvent.click(await screen.findByTestId("btn-finalizar-juego"));
    expect(onJuegoAvanzado).toHaveBeenCalled();
  });

  it("avanzar pregunta refetchea la pregunta", async () => {
    vi.mocked(getPreguntaActual)
      .mockResolvedValueOnce(pregunta)
      .mockResolvedValue({ ...pregunta, preguntaId: "q2", orden: 2, texto: "3+3?" });
    vi.mocked(getRankingJuego).mockResolvedValue(ranking);
    vi.mocked(avanzarPregunta).mockResolvedValue({
      partidaId: "p1", preguntaCerradaOrden: 1, preguntaActivadaOrden: 2, sinMasPreguntas: false
    });
    renderPanel();

    await userEvent.click(await screen.findByTestId("btn-avanzar-pregunta"));
    expect(await screen.findByText("3+3?")).toBeInTheDocument();
  });

  it("ranking 404 muestra leyenda sin datos", async () => {
    vi.mocked(getPreguntaActual).mockResolvedValue(pregunta);
    const { PuntuacionesApiError } = await import("../../api/puntuacionesApi");
    vi.mocked(getRankingJuego).mockRejectedValue(new PuntuacionesApiError("no proyectado", 404));
    renderPanel();

    expect(await screen.findByText(/sin datos de ranking/i)).toBeInTheDocument();
  });

  it("cambio de refetchSignal refetchea", async () => {
    vi.mocked(getPreguntaActual).mockResolvedValue(pregunta);
    vi.mocked(getRankingJuego).mockResolvedValue(ranking);
    const { rerender } = renderPanel();
    await screen.findByTestId("pregunta-activa");
    expect(vi.mocked(getPreguntaActual)).toHaveBeenCalledTimes(1);

    rerender(
      <TriviaRuntimePanel
        partidaId="p1" juegoId="j1" accessToken="tok" preguntasConfig={config}
        refetchSignal={1} onTerminada={vi.fn()} onJuegoAvanzado={vi.fn()}
      />
    );
    await vi.waitFor(() => expect(vi.mocked(getPreguntaActual)).toHaveBeenCalledTimes(2));
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd frontend && npx vitest run src/features/partidas/TriviaRuntimePanel.test.tsx`
Expected: FAIL — no existe `./TriviaRuntimePanel`.

- [ ] **Step 3: Write minimal implementation**

`frontend/src/features/partidas/TriviaRuntimePanel.tsx` — estructura obligatoria (JSX con clases del slice: `stack`, `question-card`, `q-title`, `muted`, `table-wrap`, `notice error`, `secondary-button`, `badge`):

```tsx
// Runtime Trivia del operador: pregunta activa + avance + finalizar + ranking del juego.
import { useCallback, useEffect, useState } from "react";
import {
  avanzarPregunta,
  finalizarJuegoActual,
  getPreguntaActual,
  OperacionesApiError,
  type PreguntaActualDto
} from "../../api/operacionesApi";
import { getRankingJuego, type RankingJuegoDto } from "../../api/puntuacionesApi";
import type { PreguntaDetail } from "../../api/partidasApi";

export interface TriviaRuntimePanelProps {
  partidaId: string;
  juegoId: string;
  accessToken: string;
  preguntasConfig: PreguntaDetail[];
  refetchSignal: number;
  onTerminada: () => void;
  onJuegoAvanzado: () => void;
}

type PreguntaVista =
  | { status: "cargando" }
  | { status: "activa"; pregunta: PreguntaActualDto }
  | { status: "sin-pregunta" }
  | { status: "error"; message: string };

export function TriviaRuntimePanel(props: TriviaRuntimePanelProps) {
  const { partidaId, juegoId, accessToken, preguntasConfig, refetchSignal, onTerminada, onJuegoAvanzado } = props;
  const [preguntaVista, setPreguntaVista] = useState<PreguntaVista>({ status: "cargando" });
  const [ranking, setRanking] = useState<RankingJuegoDto | null>(null);
  const [posteando, setPosteando] = useState(false);
  const [tick, setTick] = useState(0); // refetch interno tras avance/finalizar fallido

  const refetch = useCallback(() => setTick((t) => t + 1), []);

  useEffect(() => {
    let active = true;
    getPreguntaActual(partidaId, accessToken)
      .then((pregunta) => {
        if (active) setPreguntaVista({ status: "activa", pregunta });
      })
      .catch((caught) => {
        if (!active) return;
        if (caught instanceof OperacionesApiError && caught.statusCode === 409) {
          setPreguntaVista({ status: "sin-pregunta" });
        } else {
          setPreguntaVista({
            status: "error",
            message: caught instanceof Error ? caught.message : "Error al consultar la pregunta."
          });
        }
      });
    getRankingJuego(partidaId, juegoId, accessToken)
      .then((r) => {
        if (active) setRanking(r);
      })
      .catch(() => {
        if (active) setRanking(null);
      });
    return () => {
      active = false;
    };
  }, [partidaId, juegoId, accessToken, refetchSignal, tick]);

  async function onAvanzar() {
    setPosteando(true);
    try {
      await avanzarPregunta(partidaId, accessToken);
    } catch {
      // 409 de carrera/barrido: el refetch de abajo trae el estado real.
    } finally {
      setPosteando(false);
      refetch();
    }
  }

  async function onFinalizar() {
    setPosteando(true);
    try {
      const r = await finalizarJuegoActual(partidaId, accessToken);
      if (r.terminada) {
        onTerminada();
        return;
      }
      if (r.juegoActivadoOrden != null) {
        onJuegoAvanzado();
        return;
      }
      refetch();
    } catch {
      refetch();
    } finally {
      setPosteando(false);
    }
  }

  return (
    <div className="stack" data-testid="trivia-runtime">
      {preguntaVista.status === "cargando" ? <p className="muted">Cargando pregunta…</p> : null}
      {preguntaVista.status === "error" ? (
        <div className="notice error" role="alert">{preguntaVista.message}</div>
      ) : null}
      {preguntaVista.status === "activa" ? (
        <PreguntaActivaView
          pregunta={preguntaVista.pregunta}
          preguntasConfig={preguntasConfig}
          posteando={posteando}
          onAvanzar={() => void onAvanzar()}
        />
      ) : null}
      {preguntaVista.status === "sin-pregunta" ? (
        <div className="stack" data-testid="sin-pregunta-activa">
          <p className="muted">Sin pregunta activa.</p>
          <button type="button" data-testid="btn-finalizar-juego" disabled={posteando} onClick={() => void onFinalizar()}>
            Finalizar juego
          </button>
        </div>
      ) : null}
      <RankingView ranking={ranking} />
    </div>
  );
}
```

`PreguntaActivaView`: card `data-testid="pregunta-activa"` con `Pregunta {orden} — {texto}`, countdown `data-testid="pregunta-countdown"` (mismo patrón `Countdown` de `SesionOperadorPage` — duplicar el componente local de ~10 líneas; target = `fechaActivacion + tiempoLimiteSegundos*1000`), lista de opciones donde la correcta lleva `data-testid="opcion-correcta"` y badge "Correcta". Cruce: `const cfg = preguntasConfig.find(p => p.preguntaId === pregunta.preguntaId) ?? preguntasConfig.find(p => p.texto === pregunta.texto)`; correcta = `cfg?.opciones.find(o => o.esCorrecta)`; una opción del runtime es la correcta si `opcionId` coincide o, en su defecto, si `texto` coincide. Botón `data-testid="btn-avanzar-pregunta"` "Cerrar y avanzar" `disabled={posteando}`.

`RankingView`: si `ranking === null` o `entradas.length === 0` → `<p className="muted">Sin datos de ranking todavía.</p>`; si hay entradas → `table-wrap` + tabla `data-testid="ranking-juego"` con columnas Posición · Competidor (`competidorId.slice(0, 8)`) · Puntos · Tiempo (`mm:ss` desde `tiempoAcumuladoMs`: `Math.floor(ms/60000)` y `Math.floor(ms/1000)%60` con padStart) · Ganadas.

- [ ] **Step 4: Run test + typecheck + suite**

Run: `cd frontend && npx vitest run src/features/partidas/TriviaRuntimePanel.test.tsx && npx tsc -b && npm test`
Expected: PASS (6 tests nuevos) + tsc limpio + suite completa verde. Borrar artifacts.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/partidas/TriviaRuntimePanel.tsx frontend/src/features/partidas/TriviaRuntimePanel.test.tsx
git commit -m "$(printf 'feat(web): TriviaRuntimePanel pregunta+avance+finalizar+ranking (bloque 2c2)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>')"
```

---

### Task 5: Integración en `SesionOperadorPage` (F1 seq-guard + F5 pills + panel)

**Files:**
- Modify: `frontend/src/features/partidas/SesionOperadorPage.tsx`
- Modify: `frontend/src/features/partidas/SesionOperadorPage.test.tsx`

**Interfaces:**
- Consumes: `TriviaRuntimePanel` (Task 4); handlers nuevos del hook (Task 3); `PartidaDetail`/`JuegoDetail` de `partidasApi`.
- Produces: vista `iniciada` ahora es `{ status: "iniciada"; estado: EstadoSesionDto; config: PartidaDetail | null }`.

**Cambios obligatorios (sobre el archivo actual, líneas citadas de HEAD):**

1. **F1 seq-guard** en `cargar()` (`SesionOperadorPage.tsx:40-80`): añadir `const seqRef = useRef(0);` en el componente; `cargar` abre con `const my = ++seqRef.current;` y cada `setVista(...)` dentro de `cargar` (y su catch) se protege con `if (my !== seqRef.current) return;` inmediatamente antes.
2. **Config en iniciada:** en la rama `estado.estado === "Iniciada"`, cargar también la config: `const config = await getPartida(partidaId, accessToken).catch(() => null);` y `setVista({ status: "iniciada", estado, config })`. Actualizar el tipo `Vista`.
3. **refetchSignal:** `const [refetchSignal, setRefetchSignal] = useState(0);` Handlers nuevos: `onPreguntaActivada: () => setRefetchSignal((s) => s + 1)`, `onPreguntaCerrada: () => setRefetchSignal((s) => s + 1)`; `onJuegoActivado` pasa a `() => { setRefetchSignal((s) => s + 1); void cargar(); }`.
4. **IniciadaView** (`SesionOperadorPage.tsx:255-278`): recibe `estado`, `config`, `accessToken`, `partidaId`, `refetchSignal`, `onTerminada` (setVista terminada) y `onJuegoAvanzado` (`cargar`). **F5:** pill por juego según `juego.estado`: `"Activo"` → `pill--live`, `"Finalizado"` → `pill--done`, resto (`"Pendiente"`) → `pill--lobby`. Debajo de la lista: juego actual = `juegos.find(j => j.orden === estado.juegoActualOrden)`; si `tipoJuego === "Trivia"` → montar `TriviaRuntimePanel` con `juegoId` del juego actual y `preguntasConfig = config?.juegos.find(j => j.orden === estado.juegoActualOrden)?.trivia?.preguntas ?? []`; si `tipoJuego === "BusquedaDelTesoro"` → `<p className="muted" data-testid="runtime-bdt-placeholder">El runtime BDT llega en 2c-3.</p>`; sin juego actual → nada extra. Eliminar la nota vieja "El runtime del juego (preguntas/etapas) llega en 2c-2/2c-3.".

- [ ] **Step 1: Write the failing tests**

En `SesionOperadorPage.test.tsx`: añadir `vi.mock("./TriviaRuntimePanel", () => ({ TriviaRuntimePanel: vi.fn(() => <div data-testid="trivia-runtime-mock" />) }))` y casos:

```tsx
  it("con juego actual Trivia monta el panel de runtime", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue({
      ...estadoLobby,
      estado: "Iniciada",
      juegos: [{ juegoId: "j1", orden: 1, tipoJuego: "Trivia", estado: "Activo" }],
      juegoActualOrden: 1
    });
    vi.mocked(getPartida).mockResolvedValue(configManual);
    renderPage();
    expect(await screen.findByTestId("trivia-runtime-mock")).toBeInTheDocument();
  });

  it("con juego actual BDT muestra el placeholder de 2c-3", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue({
      ...estadoLobby,
      estado: "Iniciada",
      juegos: [{ juegoId: "j1", orden: 1, tipoJuego: "BusquedaDelTesoro", estado: "Activo" }],
      juegoActualOrden: 1
    });
    vi.mocked(getPartida).mockResolvedValue(configManual);
    renderPage();
    expect(await screen.findByTestId("runtime-bdt-placeholder")).toBeInTheDocument();
    expect(screen.queryByTestId("trivia-runtime-mock")).not.toBeInTheDocument();
  });

  it("pinta pills por estado del juego (Activo live, Finalizado done, Pendiente lobby)", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue({
      ...estadoLobby,
      estado: "Iniciada",
      juegos: [
        { juegoId: "j1", orden: 1, tipoJuego: "Trivia", estado: "Finalizado" },
        { juegoId: "j2", orden: 2, tipoJuego: "Trivia", estado: "Activo" },
        { juegoId: "j3", orden: 3, tipoJuego: "BusquedaDelTesoro", estado: "Pendiente" }
      ],
      juegoActualOrden: 2
    });
    vi.mocked(getPartida).mockResolvedValue(configManual);
    renderPage();
    const actual = await screen.findByTestId("juego-actual");
    expect(actual.className).toContain("pill--live");
    expect(screen.getByText(/Juego 1/).closest(".pill")?.className).toContain("pill--done");
    expect(screen.getByText(/Juego 3/).closest(".pill")?.className).toContain("pill--lobby");
  });

  it("seq-guard: una carga vieja que resuelve tarde no pisa la vista nueva", async () => {
    // 1a carga: lenta, resolvera Lobby. 2a carga (via push onIniciada): rapida, Iniciada.
    let resolveSlow: (v: EstadoSesionDto) => void;
    const slow = new Promise<EstadoSesionDto>((res) => (resolveSlow = res));
    vi.mocked(getEstadoSesion)
      .mockReturnValueOnce(slow)
      .mockResolvedValueOnce({
        ...estadoLobby,
        estado: "Iniciada",
        juegos: [{ juegoId: "j1", orden: 1, tipoJuego: "Trivia", estado: "Activo" }],
        juegoActualOrden: 1
      });
    vi.mocked(getPartida).mockResolvedValue(configManual);
    vi.mocked(getLobby).mockResolvedValue(lobby);

    let capturedHandlers: SesionHubHandlers = {};
    vi.mocked(useSesionHub).mockImplementation((_id, _tok, handlers) => {
      capturedHandlers = handlers;
    });

    renderPage();
    // Segunda carga completa (push) mientras la primera sigue pendiente:
    await act(async () => {
      capturedHandlers.onIniciada?.({ partidaId: "p1" });
    });
    expect(await screen.findByTestId("sesion-iniciada")).toBeInTheDocument();
    // Ahora resuelve la carga vieja con Lobby: NO debe pisar la vista iniciada.
    await act(async () => {
      resolveSlow!(estadoLobby);
    });
    expect(screen.getByTestId("sesion-iniciada")).toBeInTheDocument();
    expect(screen.queryByTestId("lobby-panel")).not.toBeInTheDocument();
  });
```

(Imports nuevos en el test: `act` de `@testing-library/react`, `useSesionHub` + `type SesionHubHandlers` de `./useSesionHub` — el mock existente `vi.mock("./useSesionHub", ...)` debe exportar también `useSesionHub: vi.fn()`; ajustar el mock actual si solo mockea el default shape.)

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd frontend && npx vitest run src/features/partidas/SesionOperadorPage.test.tsx`
Expected: FAIL — `trivia-runtime-mock`/`runtime-bdt-placeholder` no existen; pills fijas; seq-guard ausente (la vista lobby pisa la iniciada).

- [ ] **Step 3: Implement**

Aplicar los 4 cambios de "Cambios obligatorios". Los tests previos del archivo deben seguir pasando: la vista `iniciada` ahora también llama `getPartida` — los mocks existentes ya lo resuelven (`configManual`); si algún caso previo no mockeaba `getPartida` para el path iniciada, añadirle el mock mínimo sin cambiar sus asserts.

- [ ] **Step 4: Run tests + typecheck + suite completa**

Run: `cd frontend && npx vitest run src/features/partidas/SesionOperadorPage.test.tsx && npx tsc -b && npm test`
Expected: PASS (10 tests: 6 previos + 4 nuevos) + tsc limpio + suite verde. Borrar artifacts.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/partidas/SesionOperadorPage.tsx frontend/src/features/partidas/SesionOperadorPage.test.tsx
git commit -m "$(printf 'feat(web): consola monta runtime trivia + seq-guard + pills por estado (bloque 2c2)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>')"
```

---

### Task 6: Gate E2E vivo + traceability (controller)

**Files:**
- Modify: `docs/04-sdd/traceability-matrix.md` (fila 2c-2)
- Modify: `GUIA-LEVANTAMIENTO.md` (ampliar la sección de la consola: runtime Trivia requiere además Puntuaciones + RabbitMQ)
- Scratch (no commit): scripts E2E en scratchpad.

**Gate operado por el controlador.** Stack: infra compose (postgres/rabbitmq/keycloak) + partidas + operaciones-sesion + **puntuaciones** + gateway. `services/puntuaciones/.env` **no existe** (verificado): crearlo desde su `.env.example` (puerto 5030) + `KEYCLOAK_*` (patrón partidas/operaciones) + vars `RabbitMq__*` según GUIA §Event Broker; RabbitMQ habilitado también en operaciones (`RabbitMq__Enabled=true` en su `.env` si no está).

- [ ] **Step 1: Levantar stack y tokens** (script PKCE `get-token.sh` del scratchpad — recrearlo si no existe; usuarios operador/operador y participante/participante).

- [ ] **Step 2: E2E flujo Trivia completo (vía :5080)**
1. Operador: `POST /partidas` (Manual, min 1, max 10) → 201; `POST .../juegos/trivia` con **2 preguntas** (campo `puntaje`, no `puntajeAsignado`) → 201.
2. Operador: `POST /operaciones-sesion/partidas/{id}/publicacion` → 201.
3. Participante: `POST /operaciones-sesion/partidas/{id}/inscripciones` → 201.
4. Operador: `POST .../inicio` → 200 `estado=Iniciada`.
5. Operador: `GET .../pregunta-actual` → 200; **verificar si `opcionId` coincide con el de config** (`GET /partidas/{id}`) — documenta cuál cruce aplica (id o texto).
6. Participante: `POST .../pregunta-actual/respuesta {opcionId correcta}` → 200 `esCorrecta:true, cerroPregunta:true`.
7. Operador: `GET /puntuaciones/partidas/{id}/juegos/{juegoId}/ranking` → 200 con `entradas[0].puntos` = puntaje de la pregunta (dar ~2s a la proyección RabbitMQ).
8. Operador: `POST .../pregunta-actual/avance` → si la pregunta 2 ya se activó sola al cerrar la 1 (verificar con `GET /pregunta-actual`), usar el avance para **cerrarla**: → `sinMasPreguntas` según corresponda; llegar a "sin pregunta activa" (`GET` → 409).
9. Operador: `POST .../juego-actual/finalizacion` → 200 `terminada:true` (partida de 1 juego).
10. Policy: participante `POST .../pregunta-actual/avance` → 403.

- [ ] **Step 3: Smoke SignalR** — conexión operador suscrita: recibir `PreguntaActivada` (con `fechaLimiteUtc`) y `PreguntaCerrada` durante el flujo del Step 2 (adaptar `sesion-smoke.mjs`).

- [ ] **Step 4: Traceability + GUIA.** Fila 2c-2 (7 columnas, formato de las filas 2a/2b/2c-1). **Verificar cada hash con `git cat-file -t` antes de escribirlo.** GUIA: ampliar sección "Consola de sesión" con Puntuaciones + RabbitMQ para el ranking.

- [ ] **Step 5: Commit**

```bash
git add docs/04-sdd/traceability-matrix.md GUIA-LEVANTAMIENTO.md
git commit -m "$(printf 'docs(bloque2c2): traceability runtime trivia + GUIA puntuaciones\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>')"
```

---

## Self-Review (hecho por el autor del plan)

**Spec coverage:** §1 operacionesApi→T1 ✓ · §2 puntuacionesApi→T2 ✓ · §3 hook→T3 ✓ · §4 panel (activa/sin-pregunta/ranking/countdown/correcta/avance/finalizar)→T4 ✓ · §5 página (F1 seq-guard, F5 pills, config en iniciada, refetchSignal, placeholder BDT)→T5 ✓ · §6 errores (409 válido, 404 ranking suave, catch→refetch)→T4/T5 ✓ · §7 testing unit→T1-T5, E2E+smoke+policy→T6 ✓ · fuera de alcance intacto (nada toca BDT/hub-ranking/legacy) ✓.

**Placeholder scan:** limpio — la única vista descrita en prosa (PreguntaActivaView/RankingView de T4) lleva requisitos concretos con testids, cruce exacto y fórmulas (countdown target, mm:ss).

**Type consistency:** `PreguntaActualDto`/`AvancePreguntaResponse`/`AvanceJuegoResponse` (T1) consumidos con esos nombres en T4; `RankingJuegoDto`/`RankingEntrada`/`PuntuacionesApiError` (T2) en T4; handlers `onPreguntaActivada`/`onPreguntaCerrada` (T3) en T5; `TriviaRuntimePanelProps` (T4) instanciado en T5 con las 7 props exactas; `Vista.iniciada` gana `config: PartidaDetail | null` y `IniciadaView` la consume. ✓
