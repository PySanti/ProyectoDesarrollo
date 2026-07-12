# Bloque 2c-3 — Runtime BDT operador (web) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Llenar el runtime BDT de la consola del operador: etapa activa con countdown y avance, finalización del juego, entrega de pistas a un participante/equipo, mapa de geolocalización en vivo y ranking del juego.

**Architecture:** Consumo web puro contra el gateway `:5080`. Se extiende `operacionesApi` (3 fns + tipos) y `useSesionHub` (4 eventos), y se crean 3 componentes con frontera limpia: `GeoMapPanel` (react-leaflet + CircleMarker), `BdtRuntimePanel` (espeja TriviaRuntimePanel), `PistasPanel` (selector de roster). La página acumula ubicaciones en un `Map` desde el hub y monta los 3 paneles cuando el juego actual es BDT. Reusa F1 seq-guard / F5 pills de 2c-2. Ranking BDT por el mismo GET disparado por señales del hub, sin 2º hub.

**Tech Stack:** React 18 + Vite + TypeScript, react-router-dom v6, `@microsoft/signalr` (ya), **`leaflet` + `react-leaflet` + `@types/leaflet` (nuevas)**, vitest + @testing-library/react + user-event.

## Global Constraints

- Todo el tráfico backend vía gateway; base URL desde `import.meta.env.VITE_GATEWAY_BASE_URL` (patrón `resolveBaseUrl` idéntico a los otros módulos api).
- Deps nuevas permitidas SOLO en este slice: `leaflet@^1.9`, `react-leaflet@^4` (NO v5 — exige React 19; el repo es React 18.3.1), `@types/leaflet`. Ninguna otra.
- El operador **no** valida QR ni ve `codigoQREsperado` (participant-safe). No implementar subida de tesoro.
- `409` en `GET /etapa-actual` = estado válido "sin etapa activa", nunca fatal. `404` en ranking (best-effort ADR-0012) = "sin datos todavía". `POST /pistas` exige **exactamente un** destino (participante XOR equipo) o `400`.
- Enums como string; JSON camelCase.
- No romper testids/labels/aria existentes. testids nuevos (fuente): `bdt-runtime`, `etapa-activa`, `sin-etapa-activa`, `btn-avanzar-etapa`, `btn-finalizar-juego`, `ranking-juego` (reuso), `geo-map`, `geo-marker`, `pistas-panel`, `pista-destino`, `pista-texto`, `btn-enviar-pista`, `pista-enviada`.
- Typecheck gate: **`npx tsc -b`** desde `frontend/` (salida vacía = limpio; borrar artifacts untracked que deja: `tsconfig.*.tsbuildinfo`, `vite.config.js/.d.ts`, `vitest.config.js/.d.ts`). NUNCA `tsc --noEmit` pelado.
- Commits terminan con trailer exacto: `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- Subagentes: solo `git add <ruta>` archivo por archivo. PROHIBIDO `git stash/reset/checkout/restore/clean`.
- `npm test` (vitest run) verde antes de cada commit.

---

### Task 1: Extender `operacionesApi` (etapa actual, avance, pistas)

**Files:**
- Modify: `frontend/src/api/operacionesApi.ts`
- Modify: `frontend/src/api/operacionesApi.test.ts`

**Interfaces:**
- Consumes: `request<T>`/`buildAuthHeaders`/`OperacionesApiError` ya existentes.
- Produces (consumidos por T4/T5/T6):
  - `interface EtapaActualDto { partidaId: string; juegoId: string; etapaId: string; orden: number; areaBusqueda: string; tiempoLimiteSegundos: number; fechaActivacion: string }`
  - `interface AvanceEtapaResponse { partidaId: string; etapaCerradaOrden: number; etapaActivadaOrden?: number | null; sinMasEtapas: boolean }`
  - `interface EnviarPistaRequest { texto: string; participanteDestinoId?: string; equipoDestinoId?: string }`
  - `interface PistaEnviadaResponse { partidaId: string; juegoId: string; participanteDestinoId?: string | null; equipoDestinoId?: string | null; timestampUtc: string }`
  - `getEtapaActual(partidaId, accessToken, fetchImpl?): Promise<EtapaActualDto>`
  - `avanzarEtapa(partidaId, accessToken, fetchImpl?): Promise<AvanceEtapaResponse>`
  - `enviarPista(partidaId, body: EnviarPistaRequest, accessToken, fetchImpl?): Promise<PistaEnviadaResponse>`
  - `LobbyDto.participantes` cambia de `unknown[]` a `string[]`.

- [ ] **Step 1: Write the failing tests**

Añadir al `describe("operacionesApi", ...)` de `frontend/src/api/operacionesApi.test.ts` (imports nuevos: `avanzarEtapa`, `enviarPista`, `getEtapaActual`):

```ts
  it("getEtapaActual hace GET a etapa-actual; 409 lanza error con statusCode", async () => {
    const ok = okJson({
      partidaId: "p1", juegoId: "j1", etapaId: "e1", orden: 1,
      areaBusqueda: "Plaza central", tiempoLimiteSegundos: 120, fechaActivacion: "2026-07-08T12:00:00Z"
    });
    const r = await getEtapaActual("p1", "tok", ok);
    expect(r.areaBusqueda).toBe("Plaza central");
    expect(ok.mock.calls[0][0]).toBe("https://gw.example.test/operaciones-sesion/partidas/p1/etapa-actual");
    expect(ok.mock.calls[0][1].method).toBe("GET");
    const sin = okJson({ message: "sin etapa activa" }, 409);
    await expect(getEtapaActual("p1", "tok", sin)).rejects.toMatchObject({ statusCode: 409 });
  });

  it("avanzarEtapa hace POST al avance y devuelve sinMasEtapas", async () => {
    const f = okJson({ partidaId: "p1", etapaCerradaOrden: 2, etapaActivadaOrden: null, sinMasEtapas: true });
    const r = await avanzarEtapa("p1", "tok", f);
    expect(r.sinMasEtapas).toBe(true);
    expect(f.mock.calls[0][0]).toBe("https://gw.example.test/operaciones-sesion/partidas/p1/etapa-actual/avance");
    expect(f.mock.calls[0][1].method).toBe("POST");
  });

  it("enviarPista hace POST a pistas con el cuerpo y devuelve timestamp", async () => {
    const f = okJson({ partidaId: "p1", juegoId: "j1", participanteDestinoId: "u1", equipoDestinoId: null, timestampUtc: "2026-07-08T12:00:00Z" });
    const r = await enviarPista("p1", { texto: "busca cerca del arbol", participanteDestinoId: "u1" }, "tok", f);
    expect(r.participanteDestinoId).toBe("u1");
    expect(f.mock.calls[0][0]).toBe("https://gw.example.test/operaciones-sesion/partidas/p1/pistas");
    expect(f.mock.calls[0][1].method).toBe("POST");
    expect(JSON.parse(f.mock.calls[0][1].body as string)).toMatchObject({ texto: "busca cerca del arbol", participanteDestinoId: "u1" });
  });
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd frontend && npx vitest run src/api/operacionesApi.test.ts`
Expected: FAIL — funciones no exportadas.

- [ ] **Step 3: Write minimal implementation**

En `frontend/src/api/operacionesApi.ts`: cambiar el campo de `LobbyDto`:

```ts
  participantes: string[];
```

(reemplaza `participantes: unknown[];`). Y añadir al final los tipos + funciones:

```ts
export interface EtapaActualDto {
  partidaId: string;
  juegoId: string;
  etapaId: string;
  orden: number;
  areaBusqueda: string;
  tiempoLimiteSegundos: number;
  fechaActivacion: string;
}

export interface AvanceEtapaResponse {
  partidaId: string;
  etapaCerradaOrden: number;
  etapaActivadaOrden?: number | null;
  sinMasEtapas: boolean;
}

export interface EnviarPistaRequest {
  texto: string;
  participanteDestinoId?: string;
  equipoDestinoId?: string;
}

export interface PistaEnviadaResponse {
  partidaId: string;
  juegoId: string;
  participanteDestinoId?: string | null;
  equipoDestinoId?: string | null;
  timestampUtc: string;
}

export async function getEtapaActual(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<EtapaActualDto> {
  return request<EtapaActualDto>(
    `/operaciones-sesion/partidas/${partidaId}/etapa-actual`,
    { method: "GET", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}

export async function avanzarEtapa(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<AvanceEtapaResponse> {
  return request<AvanceEtapaResponse>(
    `/operaciones-sesion/partidas/${partidaId}/etapa-actual/avance`,
    { method: "POST", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}

export async function enviarPista(
  partidaId: string,
  body: EnviarPistaRequest,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<PistaEnviadaResponse> {
  return request<PistaEnviadaResponse>(
    `/operaciones-sesion/partidas/${partidaId}/pistas`,
    { method: "POST", headers: buildAuthHeaders(accessToken), body: JSON.stringify(body) },
    fetchImpl
  );
}
```

- [ ] **Step 4: Run tests + typecheck**

Run: `cd frontend && npx vitest run src/api/operacionesApi.test.ts && npx tsc -b`
Expected: PASS (11 tests: 8 previos + 3 nuevos) + tsc sin salida. Si `tsc -b` reporta un error en algún consumidor de `participantes` (no debería — la página solo usa `.length`), arreglarlo mínimo. Borrar artifacts.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/api/operacionesApi.ts frontend/src/api/operacionesApi.test.ts
git commit -m "$(printf 'feat(web): operacionesApi runtime bdt — etapa, avance, pistas (bloque 2c3)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>')"
```

---

### Task 2: Extender `useSesionHub` (etapas + ubicación)

**Files:**
- Modify: `frontend/src/features/partidas/useSesionHub.ts`
- Modify: `frontend/src/features/partidas/useSesionHub.test.ts`

**Interfaces:**
- Produces (consumidos por T6):
  - `SesionHubHandlers` gana:
    - `onEtapaActivada?: (p: { partidaId: string; juegoId: string; etapaId: string; orden: number; fechaLimiteUtc: string }) => void`
    - `onEtapaCerrada?: (p: { partidaId: string; juegoId: string; etapaId: string }) => void`
    - `onEtapaGanada?: (p: { partidaId: string; juegoId: string; etapaId: string }) => void`
    - `onUbicacionActualizada?: (p: { partidaId: string; participanteId: string; latitud: number; longitud: number; timestampUtc: string }) => void`

- [ ] **Step 1: Write the failing test**

Añadir al `describe("useSesionHub", ...)`:

```ts
  it("rutea EtapaActivada/EtapaCerrada/EtapaGanada/UbicacionActualizada a sus handlers", async () => {
    const conn = fakeConnection();
    vi.mocked(crearSesionHub).mockReturnValue(conn as never);
    const onEtapaActivada = vi.fn();
    const onEtapaCerrada = vi.fn();
    const onEtapaGanada = vi.fn();
    const onUbicacionActualizada = vi.fn();

    renderHook(() => useSesionHub("p1", "tok", { onEtapaActivada, onEtapaCerrada, onEtapaGanada, onUbicacionActualizada }));
    await Promise.resolve();

    conn.handlers["EtapaActivada"]({ partidaId: "p1", juegoId: "j1", etapaId: "e1", orden: 1, fechaLimiteUtc: "2026-07-08T12:02:00Z" });
    expect(onEtapaActivada).toHaveBeenCalledWith(expect.objectContaining({ etapaId: "e1", fechaLimiteUtc: "2026-07-08T12:02:00Z" }));
    conn.handlers["EtapaCerrada"]({ partidaId: "p1", juegoId: "j1", etapaId: "e1" });
    expect(onEtapaCerrada).toHaveBeenCalledWith(expect.objectContaining({ etapaId: "e1" }));
    conn.handlers["EtapaGanada"]({ partidaId: "p1", juegoId: "j1", etapaId: "e1" });
    expect(onEtapaGanada).toHaveBeenCalledWith(expect.objectContaining({ etapaId: "e1" }));
    conn.handlers["UbicacionActualizada"]({ partidaId: "p1", participanteId: "u1", latitud: 10.5, longitud: -66.9, timestampUtc: "2026-07-08T12:00:00Z" });
    expect(onUbicacionActualizada).toHaveBeenCalledWith(expect.objectContaining({ participanteId: "u1", latitud: 10.5, longitud: -66.9 }));
  });
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd frontend && npx vitest run src/features/partidas/useSesionHub.test.ts`
Expected: FAIL — `conn.handlers["EtapaActivada"]` undefined.

- [ ] **Step 3: Write minimal implementation**

En `useSesionHub.ts`, añadir a `SesionHubHandlers`:

```ts
  onEtapaActivada?: (payload: {
    partidaId: string;
    juegoId: string;
    etapaId: string;
    orden: number;
    fechaLimiteUtc: string;
  }) => void;
  onEtapaCerrada?: (payload: { partidaId: string; juegoId: string; etapaId: string }) => void;
  onEtapaGanada?: (payload: { partidaId: string; juegoId: string; etapaId: string }) => void;
  onUbicacionActualizada?: (payload: {
    partidaId: string;
    participanteId: string;
    latitud: number;
    longitud: number;
    timestampUtc: string;
  }) => void;
```

Y junto a los otros `connection.on(...)` (antes de `onreconnected`):

```ts
    connection.on("EtapaActivada", (p) => handlersRef.current.onEtapaActivada?.(p));
    connection.on("EtapaCerrada", (p) => handlersRef.current.onEtapaCerrada?.(p));
    connection.on("EtapaGanada", (p) => handlersRef.current.onEtapaGanada?.(p));
    connection.on("UbicacionActualizada", (p) => handlersRef.current.onUbicacionActualizada?.(p));
```

- [ ] **Step 4: Run test + typecheck**

Run: `cd frontend && npx vitest run src/features/partidas/useSesionHub.test.ts && npx tsc -b`
Expected: PASS + tsc limpio. Borrar artifacts.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/partidas/useSesionHub.ts frontend/src/features/partidas/useSesionHub.test.ts
git commit -m "$(printf 'feat(web): useSesionHub rutea eventos de etapa BDT + ubicacion (bloque 2c3)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>')"
```

---

### Task 3: Instalar leaflet + `GeoMapPanel`

**Files:**
- Modify: `frontend/package.json` + `frontend/package-lock.json` (npm)
- Create: `frontend/src/features/partidas/GeoMapPanel.tsx`
- Test: `frontend/src/features/partidas/GeoMapPanel.test.tsx`

**Interfaces:**
- Produces (consumidos por T6):
  - `interface UbicacionParticipante { participanteId: string; latitud: number; longitud: number; timestampUtc: string }`
  - `calcularCentro(ubicaciones: UbicacionParticipante[]): [number, number]`
  - `GeoMapPanel({ ubicaciones }: { ubicaciones: UbicacionParticipante[] })`

- [ ] **Step 1: Instalar dependencias**

Run: `cd frontend && npm install leaflet@^1.9 react-leaflet@^4 && npm install -D @types/leaflet`
Expected: `leaflet` (^1.9) + `react-leaflet` (^4.2.1) en `dependencies`, `@types/leaflet` en `devDependencies`, lockfile actualizado. No tocar otras deps.
**CRÍTICO:** pinear `react-leaflet@^4` — la última (v5) exige React 19 y este repo usa React 18.3.1; `npm install react-leaflet` sin versión traería v5 y rompería el peer/typecheck. v4.2.1 tiene peer `react@^18` + `leaflet@^1.9`.

- [ ] **Step 2: Write the failing test**

`frontend/src/features/partidas/GeoMapPanel.test.tsx`:

```tsx
import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { GeoMapPanel, calcularCentro, type UbicacionParticipante } from "./GeoMapPanel";

// leaflet no corre en jsdom: mockeamos react-leaflet con stubs que renderizan hijos.
vi.mock("react-leaflet", () => ({
  MapContainer: ({ children }: { children: React.ReactNode }) => <div data-testid="map">{children}</div>,
  TileLayer: () => <div data-testid="tile" />,
  CircleMarker: ({ children }: { children: React.ReactNode }) => <div data-testid="geo-marker">{children}</div>,
  Popup: ({ children }: { children: React.ReactNode }) => <div>{children}</div>
}));

const u = (id: string, lat: number, lng: number): UbicacionParticipante => ({
  participanteId: id, latitud: lat, longitud: lng, timestampUtc: new Date().toISOString()
});

describe("GeoMapPanel", () => {
  it("calcularCentro promedia lat/long; vacio -> [0,0]", () => {
    expect(calcularCentro([])).toEqual([0, 0]);
    expect(calcularCentro([u("a", 10, 20), u("b", 20, 40)])).toEqual([15, 30]);
  });

  it("renderiza un marcador por ubicacion", () => {
    render(<GeoMapPanel ubicaciones={[u("aaaaaaaa-1", 10, 20), u("bbbbbbbb-2", 11, 21)]} />);
    expect(screen.getByTestId("geo-map")).toBeInTheDocument();
    expect(screen.getAllByTestId("geo-marker")).toHaveLength(2);
  });

  it("sin ubicaciones muestra leyenda de espera y ningun marcador", () => {
    render(<GeoMapPanel ubicaciones={[]} />);
    expect(screen.getByText(/esperando ubicaciones/i)).toBeInTheDocument();
    expect(screen.queryAllByTestId("geo-marker")).toHaveLength(0);
  });
});
```

- [ ] **Step 3: Run test to verify it fails**

Run: `cd frontend && npx vitest run src/features/partidas/GeoMapPanel.test.tsx`
Expected: FAIL — no existe `./GeoMapPanel`.

- [ ] **Step 4: Write minimal implementation**

`frontend/src/features/partidas/GeoMapPanel.tsx`:

```tsx
// Mapa de geolocalizacion en vivo (operador-only): un CircleMarker por participante.
import { CircleMarker, MapContainer, Popup, TileLayer } from "react-leaflet";
import "leaflet/dist/leaflet.css";

export interface UbicacionParticipante {
  participanteId: string;
  latitud: number;
  longitud: number;
  timestampUtc: string;
}

export function calcularCentro(ubicaciones: UbicacionParticipante[]): [number, number] {
  if (ubicaciones.length === 0) return [0, 0];
  const lat = ubicaciones.reduce((s, u) => s + u.latitud, 0) / ubicaciones.length;
  const lng = ubicaciones.reduce((s, u) => s + u.longitud, 0) / ubicaciones.length;
  return [lat, lng];
}

function hace(timestampUtc: string): string {
  const seg = Math.max(0, Math.round((Date.now() - new Date(timestampUtc).getTime()) / 1000));
  return `${seg}s`;
}

export function GeoMapPanel({ ubicaciones }: { ubicaciones: UbicacionParticipante[] }) {
  const centro = calcularCentro(ubicaciones);
  const tieneUbicaciones = ubicaciones.length > 0;
  return (
    <div className="stack" data-testid="geo-map">
      <h3 className="q-title">Ubicaciones en vivo</h3>
      {tieneUbicaciones ? null : <p className="muted">Esperando ubicaciones…</p>}
      {/* key re-monta el mapa una sola vez al pasar de vacio a con-datos (salto a la zona). */}
      <MapContainer
        key={tieneUbicaciones ? "live" : "empty"}
        center={centro}
        zoom={tieneUbicaciones ? 15 : 2}
        style={{ height: "360px", width: "100%" }}
      >
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
          url="https://tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        {ubicaciones.map((u) => (
          <CircleMarker key={u.participanteId} center={[u.latitud, u.longitud]} radius={8}>
            <Popup>
              {u.participanteId.slice(0, 8)} · visto hace {hace(u.timestampUtc)}
            </Popup>
          </CircleMarker>
        ))}
      </MapContainer>
    </div>
  );
}
```

- [ ] **Step 5: Run test + typecheck + suite**

Run: `cd frontend && npx vitest run src/features/partidas/GeoMapPanel.test.tsx && npx tsc -b && npm test`
Expected: PASS (3 tests) + tsc limpio + suite verde. Si el import de `leaflet/dist/leaflet.css` rompe el test (vitest no procesa el css), añadir el mock del css con `vi.mock("leaflet/dist/leaflet.css", () => ({}))` al inicio del test. Borrar artifacts.

- [ ] **Step 6: Commit**

```bash
git add frontend/package.json frontend/package-lock.json frontend/src/features/partidas/GeoMapPanel.tsx frontend/src/features/partidas/GeoMapPanel.test.tsx
git commit -m "$(printf 'feat(web): GeoMapPanel mapa geoloc en vivo con react-leaflet (bloque 2c3)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>')"
```

---

### Task 4: `BdtRuntimePanel`

**Files:**
- Create: `frontend/src/features/partidas/BdtRuntimePanel.tsx`
- Test: `frontend/src/features/partidas/BdtRuntimePanel.test.tsx`

**Interfaces:**
- Consumes: `getEtapaActual`, `avanzarEtapa`, `finalizarJuegoActual`, `OperacionesApiError`, `EtapaActualDto` (T1); `getRankingJuego`, `RankingJuegoDto` (2c-2).
- Produces (consumido por T6): `BdtRuntimePanel(props)` con `interface BdtRuntimePanelProps { partidaId: string; juegoId: string; accessToken: string; refetchSignal: number; onTerminada: () => void; onJuegoAvanzado: () => void }`.

Espeja `TriviaRuntimePanel.tsx` (mismo esqueleto de efecto/`active`/`tick`/onAvanzar/onFinalizar) cambiando pregunta→etapa. testids: `bdt-runtime`, `etapa-activa`, `sin-etapa-activa`, `btn-avanzar-etapa`, `btn-finalizar-juego`, `ranking-juego`.

- [ ] **Step 1: Write the failing test**

`frontend/src/features/partidas/BdtRuntimePanel.test.tsx`:

```tsx
import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BdtRuntimePanel } from "./BdtRuntimePanel";
import {
  avanzarEtapa,
  finalizarJuegoActual,
  getEtapaActual,
  OperacionesApiError,
  type EtapaActualDto
} from "../../api/operacionesApi";
import { getRankingJuego, type RankingJuegoDto } from "../../api/puntuacionesApi";

vi.mock("../../api/operacionesApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../api/operacionesApi")>();
  return { ...actual, getEtapaActual: vi.fn(), avanzarEtapa: vi.fn(), finalizarJuegoActual: vi.fn() };
});
vi.mock("../../api/puntuacionesApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../api/puntuacionesApi")>();
  return { ...actual, getRankingJuego: vi.fn() };
});

const etapa: EtapaActualDto = {
  partidaId: "p1", juegoId: "j1", etapaId: "e1", orden: 1,
  areaBusqueda: "Plaza central", tiempoLimiteSegundos: 120, fechaActivacion: new Date().toISOString()
};
const ranking: RankingJuegoDto = {
  juegoId: "j1", tipoJuego: "BusquedaDelTesoro", generadoEn: "2026-07-08T12:00:00Z",
  entradas: [{ posicion: 1, competidorId: "abcdef12-0000-0000-0000-000000000000", tipoCompetidor: "Participante", puntos: 50, tiempoAcumuladoMs: 61000, unidadesGanadas: 1 }]
};

function renderPanel(props: Partial<Parameters<typeof BdtRuntimePanel>[0]> = {}) {
  return render(
    <BdtRuntimePanel partidaId="p1" juegoId="j1" accessToken="tok" refetchSignal={0} onTerminada={vi.fn()} onJuegoAvanzado={vi.fn()} {...props} />
  );
}

describe("BdtRuntimePanel", () => {
  beforeEach(() => {
    vi.mocked(getEtapaActual).mockReset();
    vi.mocked(avanzarEtapa).mockReset();
    vi.mocked(finalizarJuegoActual).mockReset();
    vi.mocked(getRankingJuego).mockReset();
  });

  it("con etapa activa muestra area, countdown, avance y ranking", async () => {
    vi.mocked(getEtapaActual).mockResolvedValue(etapa);
    vi.mocked(getRankingJuego).mockResolvedValue(ranking);
    renderPanel();
    expect(await screen.findByTestId("etapa-activa")).toBeInTheDocument();
    expect(screen.getByText(/Plaza central/)).toBeInTheDocument();
    expect(screen.getByTestId("btn-avanzar-etapa")).toBeInTheDocument();
    const tabla = screen.getByTestId("ranking-juego");
    expect(tabla).toHaveTextContent("abcdef12");
    expect(tabla).toHaveTextContent("50");
    expect(tabla).toHaveTextContent("01:01");
  });

  it("con 409 muestra sin-etapa-activa y Finalizar; terminada llama onTerminada", async () => {
    vi.mocked(getEtapaActual).mockRejectedValue(new OperacionesApiError("sin etapa", 409));
    vi.mocked(getRankingJuego).mockResolvedValue(ranking);
    vi.mocked(finalizarJuegoActual).mockResolvedValue({ partidaId: "p1", estado: "Terminada", juegoFinalizadoOrden: 1, juegoActivadoOrden: null, terminada: true });
    const onTerminada = vi.fn();
    renderPanel({ onTerminada });
    expect(await screen.findByTestId("sin-etapa-activa")).toBeInTheDocument();
    await userEvent.click(screen.getByTestId("btn-finalizar-juego"));
    expect(onTerminada).toHaveBeenCalled();
  });

  it("avanzar etapa refetchea la etapa", async () => {
    vi.mocked(getEtapaActual)
      .mockResolvedValueOnce(etapa)
      .mockResolvedValue({ ...etapa, etapaId: "e2", orden: 2, areaBusqueda: "Parque norte" });
    vi.mocked(getRankingJuego).mockResolvedValue(ranking);
    vi.mocked(avanzarEtapa).mockResolvedValue({ partidaId: "p1", etapaCerradaOrden: 1, etapaActivadaOrden: 2, sinMasEtapas: false });
    renderPanel();
    await userEvent.click(await screen.findByTestId("btn-avanzar-etapa"));
    expect(await screen.findByText(/Parque norte/)).toBeInTheDocument();
  });

  it("ranking 404 muestra leyenda sin datos", async () => {
    vi.mocked(getEtapaActual).mockResolvedValue(etapa);
    const { PuntuacionesApiError } = await import("../../api/puntuacionesApi");
    vi.mocked(getRankingJuego).mockRejectedValue(new PuntuacionesApiError("no proyectado", 404));
    renderPanel();
    expect(await screen.findByText(/sin datos de ranking/i)).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd frontend && npx vitest run src/features/partidas/BdtRuntimePanel.test.tsx`
Expected: FAIL — no existe `./BdtRuntimePanel`.

- [ ] **Step 3: Write minimal implementation**

`frontend/src/features/partidas/BdtRuntimePanel.tsx` — copiar la estructura de `TriviaRuntimePanel.tsx` cambiando pregunta→etapa. Lógica idéntica (efecto con `active`, `tick`, `refetch`, `onAvanzar` con `finally { setPosteando(false); refetch(); }`, `onFinalizar` con `terminada`→`onTerminada` / `juegoActivadoOrden!=null`→`onJuegoAvanzado` / else→`refetch`, `catch`→`refetch`). Diferencias:

```tsx
// Runtime BDT del operador: etapa activa + avance + finalizar + ranking del juego.
import { useCallback, useEffect, useState } from "react";
import {
  avanzarEtapa,
  finalizarJuegoActual,
  getEtapaActual,
  OperacionesApiError,
  type EtapaActualDto
} from "../../api/operacionesApi";
import { getRankingJuego, type RankingJuegoDto } from "../../api/puntuacionesApi";

export interface BdtRuntimePanelProps {
  partidaId: string;
  juegoId: string;
  accessToken: string;
  refetchSignal: number;
  onTerminada: () => void;
  onJuegoAvanzado: () => void;
}

type EtapaVista =
  | { status: "cargando" }
  | { status: "activa"; etapa: EtapaActualDto }
  | { status: "sin-etapa" }
  | { status: "error"; message: string };
```

- El efecto llama `getEtapaActual` (409→`{status:"sin-etapa"}`) y `getRankingJuego`.
- `onAvanzar` llama `avanzarEtapa`; `onFinalizar` llama `finalizarJuegoActual` (misma lógica que Trivia).
- Vista activa: card `data-testid="etapa-activa"` con `Etapa {orden}` + `<span>{etapa.areaBusqueda}</span>` (envuelto en span para `getByText` exacto, lección T4 de 2c-2), `<Countdown target={target} />` donde `target = new Date(new Date(etapa.fechaActivacion).getTime() + etapa.tiempoLimiteSegundos*1000).toISOString()`, botón `data-testid="btn-avanzar-etapa"` "Cerrar y avanzar" (`disabled={posteando}`). Duplicar el componente local `Countdown` (~10 líneas, igual que TriviaRuntimePanel) con `data-testid="etapa-countdown"`.
- Vista sin-etapa: `data-testid="sin-etapa-activa"` + botón `data-testid="btn-finalizar-juego"` (`disabled={posteando}`).
- `RankingView` idéntico al de TriviaRuntimePanel (guard `!ranking?.entradas?.length` → "Sin datos de ranking todavía"; tabla `data-testid="ranking-juego"` posición·competidor(`slice(0,8)`)·puntos·tiempo(mm:ss)·`unidadesGanadas`). Reusar el mismo JSX/formato.
- Raíz `data-testid="bdt-runtime"`.

- [ ] **Step 4: Run test + typecheck + suite**

Run: `cd frontend && npx vitest run src/features/partidas/BdtRuntimePanel.test.tsx && npx tsc -b && npm test`
Expected: PASS (4 tests) + tsc limpio + suite verde. Borrar artifacts.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/partidas/BdtRuntimePanel.tsx frontend/src/features/partidas/BdtRuntimePanel.test.tsx
git commit -m "$(printf 'feat(web): BdtRuntimePanel etapa+avance+finalizar+ranking (bloque 2c3)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>')"
```

---

### Task 5: `PistasPanel`

**Files:**
- Create: `frontend/src/features/partidas/PistasPanel.tsx`
- Test: `frontend/src/features/partidas/PistasPanel.test.tsx`

**Interfaces:**
- Consumes: `getLobby`, `enviarPista`, `OperacionesApiError`, `LobbyDto`, `EnviarPistaRequest` (T1/2c-1).
- Produces (consumido por T6): `PistasPanel({ partidaId, accessToken }: { partidaId: string; accessToken: string })`.

- [ ] **Step 1: Write the failing test**

`frontend/src/features/partidas/PistasPanel.test.tsx`:

```tsx
import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { PistasPanel } from "./PistasPanel";
import { enviarPista, getLobby, OperacionesApiError, type LobbyDto } from "../../api/operacionesApi";

vi.mock("../../api/operacionesApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../api/operacionesApi")>();
  return { ...actual, getLobby: vi.fn(), enviarPista: vi.fn() };
});

const lobbyIndividual: LobbyDto = {
  partidaId: "p1", sesionPartidaId: "s1", estado: "Iniciada", modalidad: "Individual",
  minimosParticipacion: 1, maximosParticipacion: 10, inscritosActivos: 2,
  participantes: ["11111111-aaaa", "22222222-bbbb"], equipos: []
};
const lobbyEquipo: LobbyDto = {
  ...lobbyIndividual, modalidad: "Equipo", participantes: [],
  equipos: [{ equipoId: "eq111111-cccc", convocados: 3, aceptados: 2 }]
};

describe("PistasPanel", () => {
  beforeEach(() => {
    vi.mocked(getLobby).mockReset();
    vi.mocked(enviarPista).mockReset();
  });

  it("Individual: envia pista al participante elegido", async () => {
    vi.mocked(getLobby).mockResolvedValue(lobbyIndividual);
    vi.mocked(enviarPista).mockResolvedValue({ partidaId: "p1", juegoId: "j1", participanteDestinoId: "11111111-aaaa", equipoDestinoId: null, timestampUtc: "2026-07-08T12:00:00Z" });
    render(<PistasPanel partidaId="p1" accessToken="tok" />);
    await screen.findByTestId("pistas-panel");
    await userEvent.selectOptions(screen.getByTestId("pista-destino"), "11111111-aaaa");
    await userEvent.type(screen.getByTestId("pista-texto"), "mira bajo el banco");
    await userEvent.click(screen.getByTestId("btn-enviar-pista"));
    expect(vi.mocked(enviarPista)).toHaveBeenCalledWith("p1", { texto: "mira bajo el banco", participanteDestinoId: "11111111-aaaa" }, "tok");
    expect(await screen.findByTestId("pista-enviada")).toBeInTheDocument();
  });

  it("Equipo: el destino se envia como equipoDestinoId", async () => {
    vi.mocked(getLobby).mockResolvedValue(lobbyEquipo);
    vi.mocked(enviarPista).mockResolvedValue({ partidaId: "p1", juegoId: "j1", participanteDestinoId: null, equipoDestinoId: "eq111111-cccc", timestampUtc: "2026-07-08T12:00:00Z" });
    render(<PistasPanel partidaId="p1" accessToken="tok" />);
    await screen.findByTestId("pistas-panel");
    await userEvent.selectOptions(screen.getByTestId("pista-destino"), "eq111111-cccc");
    await userEvent.type(screen.getByTestId("pista-texto"), "pista de equipo");
    await userEvent.click(screen.getByTestId("btn-enviar-pista"));
    expect(vi.mocked(enviarPista)).toHaveBeenCalledWith("p1", { texto: "pista de equipo", equipoDestinoId: "eq111111-cccc" }, "tok");
  });

  it("error 404 (destino no inscrito) se muestra inline", async () => {
    vi.mocked(getLobby).mockResolvedValue(lobbyIndividual);
    vi.mocked(enviarPista).mockRejectedValue(new OperacionesApiError("destino no inscrito", 404));
    render(<PistasPanel partidaId="p1" accessToken="tok" />);
    await screen.findByTestId("pistas-panel");
    await userEvent.selectOptions(screen.getByTestId("pista-destino"), "11111111-aaaa");
    await userEvent.type(screen.getByTestId("pista-texto"), "x");
    await userEvent.click(screen.getByTestId("btn-enviar-pista"));
    expect(await screen.findByRole("alert")).toBeInTheDocument();
  });

  it("roster vacio muestra leyenda", async () => {
    vi.mocked(getLobby).mockResolvedValue({ ...lobbyIndividual, participantes: [] });
    render(<PistasPanel partidaId="p1" accessToken="tok" />);
    expect(await screen.findByText(/sin inscritos/i)).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd frontend && npx vitest run src/features/partidas/PistasPanel.test.tsx`
Expected: FAIL — no existe `./PistasPanel`.

- [ ] **Step 3: Write minimal implementation**

`frontend/src/features/partidas/PistasPanel.tsx`. Estructura obligatoria (clases del slice: `stack`, `notice error`, `muted`; `<select>`, `<textarea>` nativos):

```tsx
// Envio de pistas del operador a un participante o equipo (BDT).
import { useEffect, useState } from "react";
import { enviarPista, getLobby, OperacionesApiError, type LobbyDto } from "../../api/operacionesApi";

export function PistasPanel({ partidaId, accessToken }: { partidaId: string; accessToken: string }) {
  const [lobby, setLobby] = useState<LobbyDto | null>(null);
  const [destino, setDestino] = useState("");
  const [texto, setTexto] = useState("");
  const [posteando, setPosteando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [enviadaEn, setEnviadaEn] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    getLobby(partidaId, accessToken)
      .then((l) => { if (active) setLobby(l); })
      .catch(() => { if (active) setLobby(null); });
    return () => { active = false; };
  }, [partidaId, accessToken]);

  const esEquipo = lobby?.modalidad === "Equipo";
  const opciones = esEquipo ? (lobby?.equipos.map((e) => e.equipoId) ?? []) : (lobby?.participantes ?? []);

  async function onEnviar() {
    if (!destino || !texto.trim()) return;
    setPosteando(true);
    setError(null);
    setEnviadaEn(null);
    try {
      const body = esEquipo
        ? { texto, equipoDestinoId: destino }
        : { texto, participanteDestinoId: destino };
      const r = await enviarPista(partidaId, body, accessToken);
      setEnviadaEn(r.timestampUtc);
      setTexto("");
    } catch (caught) {
      setError(
        caught instanceof OperacionesApiError
          ? mapPistaError(caught.statusCode)
          : "No se pudo enviar la pista."
      );
    } finally {
      setPosteando(false);
    }
  }

  return (
    <div className="stack" data-testid="pistas-panel">
      <h3 className="q-title">Enviar pista</h3>
      {opciones.length === 0 ? (
        <p className="muted">Sin inscritos para enviar pistas.</p>
      ) : (
        <>
          <select data-testid="pista-destino" value={destino} onChange={(e) => setDestino(e.target.value)}>
            <option value="">— elige {esEquipo ? "equipo" : "participante"} —</option>
            {opciones.map((id) => (
              <option key={id} value={id}>{id.slice(0, 8)}</option>
            ))}
          </select>
          <textarea data-testid="pista-texto" value={texto} onChange={(e) => setTexto(e.target.value)} placeholder="Texto de la pista" />
          <button type="button" data-testid="btn-enviar-pista" disabled={posteando || !destino || !texto.trim()} onClick={() => void onEnviar()}>
            Enviar pista
          </button>
        </>
      )}
      {error ? <div className="notice error" role="alert">{error}</div> : null}
      {enviadaEn ? <p className="muted" data-testid="pista-enviada">Pista enviada ({enviadaEn}).</p> : null}
    </div>
  );
}

function mapPistaError(statusCode: number): string {
  switch (statusCode) {
    case 400: return "Indica exactamente un destino.";
    case 404: return "El destino no tiene una inscripción activa.";
    case 409: return "No se puede enviar la pista ahora (juego no BDT o sin etapa activa).";
    default: return "No se pudo enviar la pista.";
  }
}
```

- [ ] **Step 4: Run test + typecheck + suite**

Run: `cd frontend && npx vitest run src/features/partidas/PistasPanel.test.tsx && npx tsc -b && npm test`
Expected: PASS (4 tests) + tsc limpio + suite verde. Borrar artifacts.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/partidas/PistasPanel.tsx frontend/src/features/partidas/PistasPanel.test.tsx
git commit -m "$(printf 'feat(web): PistasPanel selector de roster + envio de pista (bloque 2c3)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>')"
```

---

### Task 6: Integración en `SesionOperadorPage` (montar paneles BDT + ubicaciones)

**Files:**
- Modify: `frontend/src/features/partidas/SesionOperadorPage.tsx`
- Modify: `frontend/src/features/partidas/SesionOperadorPage.test.tsx`

**Interfaces:**
- Consumes: `BdtRuntimePanel` (T4), `PistasPanel` (T5), `GeoMapPanel` + `UbicacionParticipante` (T3); handlers nuevos del hook (T2).

**Cambios obligatorios (sobre el archivo actual, líneas de HEAD):**

1. **Imports** (junto a `TriviaRuntimePanel`): `import { BdtRuntimePanel } from "./BdtRuntimePanel";`, `import { PistasPanel } from "./PistasPanel";`, `import { GeoMapPanel, type UbicacionParticipante } from "./GeoMapPanel";`.
2. **Estado ubicaciones** en el componente (junto a `refetchSignal`, línea ~40): `const [ubicaciones, setUbicaciones] = useState<Map<string, UbicacionParticipante>>(new Map());`.
3. **Limpiar en cambio de partida** (efecto nuevo): `useEffect(() => { setUbicaciones(new Map()); }, [partidaId]);`.
4. **Handlers del hub** (objeto `handlers`, ~línea 109): añadir
   ```ts
       onEtapaActivada: () => setRefetchSignal((s) => s + 1),
       onEtapaCerrada: () => setRefetchSignal((s) => s + 1),
       onEtapaGanada: () => setRefetchSignal((s) => s + 1),
       onUbicacionActualizada: (p) =>
         setUbicaciones((prev) => new Map(prev).set(p.participanteId, p))
   ```
5. **VistaCtx** (interface ~166) gana `ubicaciones: UbicacionParticipante[]`; en la construcción del ctx (objeto de `renderVista(vista, { ... })`, ~línea 149) añadir `ubicaciones: Array.from(ubicaciones.values())`.
6. **renderVista → IniciadaView** (~línea 200): pasar `ubicaciones={ctx.ubicaciones}`.
7. **IniciadaViewProps** (~313) gana `ubicaciones: UbicacionParticipante[]`; **`IniciadaView`** (~320): reemplazar el bloque placeholder
   ```tsx
   {juegoActual?.tipoJuego === "BusquedaDelTesoro" ? (
     <p className="muted" data-testid="runtime-bdt-placeholder">El runtime BDT llega en 2c-3.</p>
   ) : null}
   ```
   por
   ```tsx
   {juegoActual?.tipoJuego === "BusquedaDelTesoro" ? (
     <div className="stack" key={juegoActual.juegoId}>
       <BdtRuntimePanel
         partidaId={partidaId}
         juegoId={juegoActual.juegoId}
         accessToken={accessToken}
         refetchSignal={refetchSignal}
         onTerminada={onTerminada}
         onJuegoAvanzado={onJuegoAvanzado}
       />
       <PistasPanel partidaId={partidaId} accessToken={accessToken} />
       <GeoMapPanel ubicaciones={ubicaciones} />
     </div>
   ) : null}
   ```
   Añadir `ubicaciones` a la desestructuración de props de `IniciadaView`.

- [ ] **Step 1: Write the failing tests**

En `SesionOperadorPage.test.tsx`: añadir mocks de los 3 paneles nuevos y casos. Junto al mock existente de `./TriviaRuntimePanel`:

```tsx
vi.mock("./BdtRuntimePanel", () => ({ BdtRuntimePanel: vi.fn(() => <div data-testid="bdt-runtime-mock" />) }));
vi.mock("./PistasPanel", () => ({ PistasPanel: vi.fn(() => <div data-testid="pistas-mock" />) }));
vi.mock("./GeoMapPanel", () => ({ GeoMapPanel: vi.fn(({ ubicaciones }: { ubicaciones: unknown[] }) => <div data-testid="geo-mock">{ubicaciones.length}</div>) }));
```

Casos nuevos (reusan `estadoLobby`/`configManual` y el mock de `useSesionHub` con `mockImplementation` para capturar handlers, ya presente en el archivo):

```tsx
  it("con juego actual BDT monta BdtRuntimePanel, PistasPanel y GeoMapPanel (no el placeholder)", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue({
      ...estadoLobby, estado: "Iniciada",
      juegos: [{ juegoId: "j1", orden: 1, tipoJuego: "BusquedaDelTesoro", estado: "Activo" }],
      juegoActualOrden: 1
    });
    vi.mocked(getPartida).mockResolvedValue(configManual);
    renderPage();
    expect(await screen.findByTestId("bdt-runtime-mock")).toBeInTheDocument();
    expect(screen.getByTestId("pistas-mock")).toBeInTheDocument();
    expect(screen.getByTestId("geo-mock")).toBeInTheDocument();
    expect(screen.queryByTestId("runtime-bdt-placeholder")).not.toBeInTheDocument();
  });

  it("un push UbicacionActualizada alimenta el GeoMapPanel", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue({
      ...estadoLobby, estado: "Iniciada",
      juegos: [{ juegoId: "j1", orden: 1, tipoJuego: "BusquedaDelTesoro", estado: "Activo" }],
      juegoActualOrden: 1
    });
    vi.mocked(getPartida).mockResolvedValue(configManual);
    let capturedHandlers: SesionHubHandlers = {};
    vi.mocked(useSesionHub).mockImplementation((_id, _tok, handlers) => { capturedHandlers = handlers; });
    renderPage();
    await screen.findByTestId("geo-mock");
    expect(screen.getByTestId("geo-mock")).toHaveTextContent("0");
    await act(async () => {
      capturedHandlers.onUbicacionActualizada?.({ partidaId: "p1", participanteId: "u1", latitud: 10, longitud: 20, timestampUtc: new Date().toISOString() });
    });
    expect(screen.getByTestId("geo-mock")).toHaveTextContent("1");
  });
```

(Import `act` de `@testing-library/react` y `type SesionHubHandlers` de `./useSesionHub` si no están ya.)

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd frontend && npx vitest run src/features/partidas/SesionOperadorPage.test.tsx`
Expected: FAIL — `bdt-runtime-mock`/`geo-mock` no montados; el placeholder sigue.

- [ ] **Step 3: Implement**

Aplicar los 7 cambios. Los casos previos del archivo deben seguir pasando (el caso previo "con juego actual BDT muestra el placeholder de 2c-3" **cambia**: ahora monta los paneles — actualizar ESE test para asertar `bdt-runtime-mock` en vez de `runtime-bdt-placeholder`, o eliminarlo si el nuevo caso lo cubre; no dejar un test que afirme el placeholder ausente).

- [ ] **Step 4: Run tests + typecheck + suite completa**

Run: `cd frontend && npx vitest run src/features/partidas/SesionOperadorPage.test.tsx && npx tsc -b && npm test`
Expected: PASS + tsc limpio + suite verde. Borrar artifacts.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/partidas/SesionOperadorPage.tsx frontend/src/features/partidas/SesionOperadorPage.test.tsx
git commit -m "$(printf 'feat(web): consola monta runtime BDT + pistas + mapa geoloc (bloque 2c3)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>')"
```

---

### Task 7: Gate E2E vivo + traceability (controller)

**Files:**
- Modify: `docs/04-sdd/traceability-matrix.md` (fila 2c-3)
- Modify: `GUIA-LEVANTAMIENTO.md` (nota: runtime BDT usa las mismas dependencias de servicio que 2c-2 + tiles OSM externos en el cliente)
- Scratch (no commit): scripts E2E.

**Gate operado por el controlador.** Stack: infra (postgres/rabbitmq/keycloak) + partidas + operaciones-sesion + puntuaciones + gateway (mismo de 2c-2).

- [ ] **Step 1: Levantar stack y tokens** (script PKCE `get-token.sh`; operador/operador + participante/participante).

- [ ] **Step 2: E2E flujo BDT (vía :5080)**
1. Operador: `POST /partidas` (Manual, min 1) → 201; `POST .../juegos/bdt` con **2 etapas** (verificar en vivo los nombres de campo del payload BDT — `areaBusqueda` a nivel juego, etapas con `codigoQREsperado`/`puntaje`/`tiempoLimiteSegundos`; ajustar como en 2c-2 con trivia `puntaje`).
2. `POST /operaciones-sesion/partidas/{id}/publicacion` 201.
3. Participante: `POST /operaciones-sesion/partidas/{id}/inscripciones` 201.
4. Operador: `POST .../inicio` 200 Iniciada.
5. Operador: `GET .../etapa-actual` 200 (confirmar **sin** `codigoQREsperado`).
6. Operador: `POST .../pistas` `{ participanteDestinoId: <participante>, texto: "..." }` → 200 + `PistaEnviadaResponse`.
7. Operador: `POST .../etapa-actual/avance` (cerrar etapa 1) → verificar etapa 2 activa (`GET .../etapa-actual`); avanzar de nuevo → `sinMasEtapas:true`.
8. Operador: `POST .../juego-actual/finalizacion` → `terminada:true`.
9. Policy: participante `POST .../etapa-actual/avance` → 403; participante `POST .../pistas` → 403.

- [ ] **Step 3: Smokes SignalR** (adaptar `trivia-smoke.mjs`):
  - (a) operador suscrito recibe `EtapaActivada {fechaLimiteUtc}` (al iniciar) + `EtapaCerrada` (al avanzar).
  - (b) segundo cliente como **participante**: `SuscribirAPartida` + `EnviarUbicacion(10.5,-66.9)` → el operador suscrito recibe `UbicacionActualizada {participanteId, latitud:10.5, longitud:-66.9}`.

- [ ] **Step 4: Traceability + GUIA.** Fila 2c-3 (7 columnas, formato 2c-1/2c-2). **Verificar cada hash con `git cat-file -t`.** GUIA: nota de que la consola BDT usa el mismo stack de servicios que 2c-2 y que el mapa carga tiles OSM externos (internet en la demo).

- [ ] **Step 5: Commit**

```bash
git add docs/04-sdd/traceability-matrix.md GUIA-LEVANTAMIENTO.md
git commit -m "$(printf 'docs(bloque2c3): traceability runtime BDT + GUIA mapa geoloc\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>')"
```

---

## Self-Review (hecho por el autor del plan)

**Spec coverage:** §1 operacionesApi (etapa/avance/pistas + participantes:string[])→T1 ✓ · §2 hook 4 eventos→T2 ✓ · §3 GeoMapPanel+leaflet→T3 ✓ · §4 BdtRuntimePanel→T4 ✓ · §5 PistasPanel→T5 ✓ · §6 página (ubicaciones Map, handlers etapa/ubicación, montaje 3 paneles, limpieza por partida, reuso F1/F5)→T6 ✓ · §7 errores (409 etapa válido, 404 ranking suave, 400/404/409 pista inline)→T4/T5 ✓ · testing unit→T1-T6, E2E+smokes+policy→T7 ✓ · fuera de alcance (QR/consolidado/legacy/hub-ranking/click-marcador) intacto ✓.

**Placeholder scan:** limpio — GeoMapPanel/PistasPanel dados verbatim; BdtRuntimePanel se especifica como espejo de TriviaRuntimePanel (ya en el repo) con las diferencias exactas + testids; sin "TBD"/"add error handling".

**Type consistency:** `EtapaActualDto`/`AvanceEtapaResponse`/`EnviarPistaRequest`/`PistaEnviadaResponse` (T1) consumidos con esos nombres en T4/T5; `LobbyDto.participantes: string[]` (T1) usado por T5; handlers `onEtapa*`/`onUbicacionActualizada` (T2) en T6; `UbicacionParticipante`/`calcularCentro`/`GeoMapPanel` (T3) en T6; `BdtRuntimePanelProps` (T4) y `PistasPanel` props (T5) instanciados en T6 con las firmas exactas; `getRankingJuego`/`RankingJuegoDto`/`finalizarJuegoActual` reusados de 2c-2. ✓
