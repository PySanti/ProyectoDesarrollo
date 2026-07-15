# Bloque 2c-1 — Consola de sesión operador (web) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dar al operador una consola web para publicar una partida a lobby, verla llenarse, iniciarla (manual/automática) y ver el shell de la sesión iniciada, con tiempo real vía SignalR.

**Architecture:** Consumo web puro contra el gateway `:5080`. Nuevo cliente HTTP `operacionesApi` (patrón `partidasApi`), nuevo cliente SignalR (`@microsoft/signalr`) envuelto en `sesionHub` + hook `useSesionHub`, y una página `SesionOperadorPage` en ruta `/partidas/:partidaId/sesion`. El detalle de partida (2b) gana la acción de publicar. Cero cambios de backend/contratos.

**Tech Stack:** React 18 + Vite + TypeScript, react-router-dom v6, `@microsoft/signalr` (nueva dep), vitest + @testing-library/react.

## Global Constraints

- Todo el tráfico backend pasa por el gateway; base URL desde `import.meta.env.VITE_GATEWAY_BASE_URL` (patrón `resolveBaseUrl` idéntico a `partidasApi.ts`).
- Única dep nueva permitida en este slice: `@microsoft/signalr@^8`. No agregar ninguna otra (ni lib de mapa — es 2c-3).
- Enums serializados como string; JSON camelCase.
- No romper testids/labels/aria de tests existentes. Los testids nuevos definidos aquí son la fuente.
- Rutas de operador bajo `RequireRole need="Operador"` (igual que las rutas `partidas` de 2b).
- Commits terminan con trailer exacto: `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- Subagentes: solo `git add <ruta>` archivo por archivo, rutas exactas. PROHIBIDO `git stash/reset/checkout/restore/clean`.
- Comandos desde `frontend/`: `npm test` (vitest run), `npx tsc --noEmit`. Verde obligatorio antes de cada commit.
- Reusar `getPartida` de `partidasApi` para `modoInicioPartida`/`tiempoInicio` (no están en los DTOs de operaciones). Duplicar el pequeño `resolveBaseUrl` en cada módulo api es el patrón existente — seguirlo, no refactorizar.

---

### Task 1: Cliente HTTP `operacionesApi`

**Files:**
- Create: `frontend/src/api/operacionesApi.ts`
- Test: `frontend/src/api/operacionesApi.test.ts`

**Interfaces:**
- Consumes: nada (primer task).
- Produces:
  - `OperacionesApiError extends Error { statusCode: number }`
  - `type EstadoSesion = "Lobby" | "Iniciada" | "Cancelada" | "Terminada"`
  - `type Modalidad = "Individual" | "Equipo"`
  - `interface LobbyEquipo { equipoId: string; convocados: number; aceptados: number }`
  - `interface LobbyDto { partidaId; sesionPartidaId; estado: EstadoSesion; modalidad: Modalidad; minimosParticipacion: number; maximosParticipacion: number; inscritosActivos: number; participantes: unknown[]; equipos: LobbyEquipo[] }`
  - `interface InicioPartidaResponse { partidaId: string; estado: EstadoSesion; juegoActivadoId?: string; juegoActivadoOrden?: number }`
  - `interface JuegoEstado { juegoId: string; orden: number; tipoJuego: "Trivia" | "BusquedaDelTesoro"; estado: string }`
  - `interface EstadoSesionDto { partidaId; sesionPartidaId; estado: EstadoSesion; modalidad: Modalidad; juegos: JuegoEstado[]; juegoActualOrden?: number }`
  - `publicarPartida(partidaId, accessToken, fetchImpl?): Promise<LobbyDto>`
  - `getLobby(partidaId, accessToken, fetchImpl?): Promise<LobbyDto>`
  - `iniciarPartida(partidaId, accessToken, fetchImpl?): Promise<InicioPartidaResponse>`
  - `getEstadoSesion(partidaId, accessToken, fetchImpl?): Promise<EstadoSesionDto>`

- [ ] **Step 1: Write the failing test**

`frontend/src/api/operacionesApi.test.ts`:

```ts
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  getEstadoSesion,
  getLobby,
  iniciarPartida,
  OperacionesApiError,
  publicarPartida
} from "./operacionesApi";

const okJson = (body: unknown, status = 200) =>
  vi.fn().mockResolvedValue(new Response(JSON.stringify(body), { status }));

describe("operacionesApi", () => {
  beforeEach(() => vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test/"));
  afterEach(() => vi.unstubAllEnvs());

  it("publicarPartida hace POST a publicacion con bearer y devuelve LobbyDto", async () => {
    const fetchImpl = okJson({ partidaId: "p1", estado: "Lobby", inscritosActivos: 0 }, 201);
    const r = await publicarPartida("p1", "tok", fetchImpl);
    expect(r.estado).toBe("Lobby");
    expect(fetchImpl.mock.calls[0][0]).toBe(
      "https://gw.example.test/operaciones-sesion/partidas/p1/publicacion"
    );
    expect(fetchImpl.mock.calls[0][1].method).toBe("POST");
    expect((fetchImpl.mock.calls[0][1].headers as Record<string, string>).Authorization).toBe(
      "Bearer tok"
    );
  });

  it("getLobby hace GET al lobby", async () => {
    const fetchImpl = okJson({ partidaId: "p1", estado: "Lobby", inscritosActivos: 2 });
    await getLobby("p1", "tok", fetchImpl);
    expect(fetchImpl.mock.calls[0][0]).toBe("https://gw.example.test/operaciones-sesion/partidas/p1/lobby");
    expect(fetchImpl.mock.calls[0][1].method).toBe("GET");
  });

  it("iniciarPartida hace POST a inicio y acepta estado Cancelada (200) como resultado", async () => {
    const fetchImpl = okJson({ partidaId: "p1", estado: "Cancelada" });
    const r = await iniciarPartida("p1", "tok", fetchImpl);
    expect(r.estado).toBe("Cancelada");
    expect(fetchImpl.mock.calls[0][0]).toBe("https://gw.example.test/operaciones-sesion/partidas/p1/inicio");
    expect(fetchImpl.mock.calls[0][1].method).toBe("POST");
  });

  it("getEstadoSesion hace GET al estado", async () => {
    const fetchImpl = okJson({ partidaId: "p1", estado: "Iniciada", modalidad: "Individual", juegos: [] });
    const r = await getEstadoSesion("p1", "tok", fetchImpl);
    expect(r.estado).toBe("Iniciada");
    expect(fetchImpl.mock.calls[0][0]).toBe("https://gw.example.test/operaciones-sesion/partidas/p1/estado");
  });

  it("error del backend lanza OperacionesApiError con status y message", async () => {
    const f409 = okJson({ message: "ya publicada" }, 409);
    await expect(publicarPartida("p1", "tok", f409)).rejects.toMatchObject({
      statusCode: 409,
      message: "ya publicada"
    });
    const f404 = okJson({ message: "no publicada" }, 404);
    await expect(getLobby("p1", "tok", f404)).rejects.toBeInstanceOf(OperacionesApiError);
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd frontend && npx vitest run src/api/operacionesApi.test.ts`
Expected: FAIL — no existe `./operacionesApi`.

- [ ] **Step 3: Write minimal implementation**

`frontend/src/api/operacionesApi.ts`:

```ts
// Cliente HTTP del servicio Operaciones de Sesion (runtime de la partida), via gateway.
export type EstadoSesion = "Lobby" | "Iniciada" | "Cancelada" | "Terminada";
export type Modalidad = "Individual" | "Equipo";

export interface LobbyEquipo {
  equipoId: string;
  convocados: number;
  aceptados: number;
}

export interface LobbyDto {
  partidaId: string;
  sesionPartidaId: string;
  estado: EstadoSesion;
  modalidad: Modalidad;
  minimosParticipacion: number;
  maximosParticipacion: number;
  inscritosActivos: number;
  participantes: unknown[];
  equipos: LobbyEquipo[];
}

export interface InicioPartidaResponse {
  partidaId: string;
  estado: EstadoSesion;
  juegoActivadoId?: string;
  juegoActivadoOrden?: number;
}

export interface JuegoEstado {
  juegoId: string;
  orden: number;
  tipoJuego: "Trivia" | "BusquedaDelTesoro";
  estado: string;
}

export interface EstadoSesionDto {
  partidaId: string;
  sesionPartidaId: string;
  estado: EstadoSesion;
  modalidad: Modalidad;
  juegos: JuegoEstado[];
  juegoActualOrden?: number;
}

export class OperacionesApiError extends Error {
  constructor(
    message: string,
    public readonly statusCode: number
  ) {
    super(message);
    this.name = "OperacionesApiError";
  }
}

function resolveBaseUrl(): string {
  const value = import.meta.env.VITE_GATEWAY_BASE_URL as string | undefined;
  if (!value) {
    throw new Error("Missing VITE_GATEWAY_BASE_URL environment variable.");
  }
  return value.replace(/\/$/, "");
}

function buildAuthHeaders(accessToken: string): HeadersInit {
  return {
    "Content-Type": "application/json",
    Authorization: `Bearer ${accessToken}`
  };
}

async function request<T>(path: string, init: RequestInit, fetchImpl: typeof fetch): Promise<T> {
  const response = await fetchImpl(`${resolveBaseUrl()}${path}`, init);
  const body = (await response.json().catch(() => ({}))) as T & { message?: string };
  if (!response.ok) {
    const message = body.message ?? `Operaciones API error. Status=${response.status}`;
    throw new OperacionesApiError(message, response.status);
  }
  return body;
}

export async function publicarPartida(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<LobbyDto> {
  return request<LobbyDto>(
    `/operaciones-sesion/partidas/${partidaId}/publicacion`,
    { method: "POST", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}

export async function getLobby(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<LobbyDto> {
  return request<LobbyDto>(
    `/operaciones-sesion/partidas/${partidaId}/lobby`,
    { method: "GET", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}

export async function iniciarPartida(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<InicioPartidaResponse> {
  return request<InicioPartidaResponse>(
    `/operaciones-sesion/partidas/${partidaId}/inicio`,
    { method: "POST", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}

export async function getEstadoSesion(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<EstadoSesionDto> {
  return request<EstadoSesionDto>(
    `/operaciones-sesion/partidas/${partidaId}/estado`,
    { method: "GET", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}
```

- [ ] **Step 4: Run test + tsc to verify they pass**

Run: `cd frontend && npx vitest run src/api/operacionesApi.test.ts && npx tsc --noEmit`
Expected: PASS (5 tests) + tsc sin errores.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/api/operacionesApi.ts frontend/src/api/operacionesApi.test.ts
git commit -m "$(printf 'feat(web): cliente operacionesApi via gateway (bloque 2c1)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>')"
```

---

### Task 2: Instalar SignalR + fábrica `sesionHub`

**Files:**
- Modify: `frontend/package.json` (dep `@microsoft/signalr`), `frontend/package-lock.json` (regenerado por npm)
- Create: `frontend/src/api/sesionHub.ts`
- Test: `frontend/src/api/sesionHub.test.ts`

**Interfaces:**
- Consumes: nada de tasks previos.
- Produces:
  - `sesionHubUrl(): string` → `${base}/operaciones-sesion/hubs/sesion`
  - `crearSesionHub(accessToken: string): HubConnection` (de `@microsoft/signalr`), con `accessTokenFactory` y `withAutomaticReconnect()`.

- [ ] **Step 1: Instalar la dependencia**

Run: `cd frontend && npm install @microsoft/signalr@^8`
Expected: `@microsoft/signalr` en `dependencies` de `package.json`, `package-lock.json` actualizado. (No tocar otras deps.)

- [ ] **Step 2: Write the failing test**

`frontend/src/api/sesionHub.test.ts`:

```ts
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

describe("sesionHub", () => {
  beforeEach(() => vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test/"));
  afterEach(() => {
    vi.unstubAllEnvs();
    vi.resetModules();
    vi.restoreAllMocks();
  });

  it("sesionHubUrl arma el prefijo operaciones-sesion del hub", async () => {
    const { sesionHubUrl } = await import("./sesionHub");
    expect(sesionHubUrl()).toBe("https://gw.example.test/operaciones-sesion/hubs/sesion");
  });

  it("crearSesionHub configura withUrl con la url del hub y accessTokenFactory que devuelve el token", async () => {
    const build = vi.fn(() => ({ __fake: true }));
    const withAutomaticReconnect = vi.fn(() => ({ build }));
    const withUrl = vi.fn(() => ({ withAutomaticReconnect }));
    const HubConnectionBuilder = vi.fn(() => ({ withUrl }));
    vi.doMock("@microsoft/signalr", () => ({ HubConnectionBuilder }));

    const { crearSesionHub, sesionHubUrl } = await import("./sesionHub");
    const conn = crearSesionHub("tok");

    expect(conn).toEqual({ __fake: true });
    expect(withUrl).toHaveBeenCalledTimes(1);
    const [url, options] = withUrl.mock.calls[0] as [string, { accessTokenFactory: () => string }];
    expect(url).toBe(sesionHubUrl());
    expect(options.accessTokenFactory()).toBe("tok");
    expect(withAutomaticReconnect).toHaveBeenCalled();
  });
});
```

- [ ] **Step 3: Run test to verify it fails**

Run: `cd frontend && npx vitest run src/api/sesionHub.test.ts`
Expected: FAIL — no existe `./sesionHub`.

- [ ] **Step 4: Write minimal implementation**

`frontend/src/api/sesionHub.ts`:

```ts
// Fabrica delgada de la conexion SignalR al hub de sesion (Operaciones de Sesion), via gateway.
import { HubConnection, HubConnectionBuilder } from "@microsoft/signalr";

function resolveBaseUrl(): string {
  const value = import.meta.env.VITE_GATEWAY_BASE_URL as string | undefined;
  if (!value) {
    throw new Error("Missing VITE_GATEWAY_BASE_URL environment variable.");
  }
  return value.replace(/\/$/, "");
}

export function sesionHubUrl(): string {
  return `${resolveBaseUrl()}/operaciones-sesion/hubs/sesion`;
}

export function crearSesionHub(accessToken: string): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(sesionHubUrl(), { accessTokenFactory: () => accessToken })
    .withAutomaticReconnect()
    .build();
}
```

- [ ] **Step 5: Run test + tsc to verify they pass**

Run: `cd frontend && npx vitest run src/api/sesionHub.test.ts && npx tsc --noEmit`
Expected: PASS (2 tests) + tsc limpio.

- [ ] **Step 6: Commit**

```bash
git add frontend/package.json frontend/package-lock.json frontend/src/api/sesionHub.ts frontend/src/api/sesionHub.test.ts
git commit -m "$(printf 'feat(web): instala @microsoft/signalr + fabrica sesionHub (bloque 2c1)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>')"
```

---

### Task 3: Hook `useSesionHub`

**Files:**
- Create: `frontend/src/features/partidas/useSesionHub.ts`
- Test: `frontend/src/features/partidas/useSesionHub.test.ts`

**Interfaces:**
- Consumes: `crearSesionHub` de `../../api/sesionHub` (Task 2).
- Produces:
  - `interface SesionHubHandlers { onEnLobby?; onIniciada?; onCancelada?; onJuegoActivado?; onFinalizada? }` con payloads:
    - `onEnLobby(p: { partidaId: string })`
    - `onIniciada(p: { partidaId: string })`
    - `onCancelada(p: { partidaId: string; motivo?: string })`
    - `onJuegoActivado(p: { partidaId: string; juegoId: string; orden: number; tipoJuego: string })`
    - `onFinalizada(p: { partidaId: string })`
  - `useSesionHub(partidaId: string, accessToken: string, handlers: SesionHubHandlers): void`

- [ ] **Step 1: Write the failing test**

`frontend/src/features/partidas/useSesionHub.test.ts`:

```ts
import { afterEach, describe, expect, it, vi } from "vitest";
import { renderHook } from "@testing-library/react";
import { useSesionHub } from "./useSesionHub";
import { crearSesionHub } from "../../api/sesionHub";

vi.mock("../../api/sesionHub", () => ({ crearSesionHub: vi.fn() }));

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

describe("useSesionHub", () => {
  afterEach(() => vi.clearAllMocks());

  it("al montar arranca, se suscribe y registra handlers; el push invoca el callback; al desmontar se desuscribe y detiene", async () => {
    const conn = fakeConnection();
    vi.mocked(crearSesionHub).mockReturnValue(conn as never);
    const onIniciada = vi.fn();

    const { unmount } = renderHook(() => useSesionHub("p1", "tok", { onIniciada }));
    await Promise.resolve();
    await Promise.resolve();

    expect(conn.start).toHaveBeenCalled();
    expect(conn.invoke).toHaveBeenCalledWith("SuscribirAPartida", "p1");
    expect(conn.on).toHaveBeenCalledWith("PartidaIniciada", expect.any(Function));

    conn.handlers["PartidaIniciada"]({ partidaId: "p1" });
    expect(onIniciada).toHaveBeenCalledWith({ partidaId: "p1" });

    unmount();
    expect(conn.invoke).toHaveBeenCalledWith("DesuscribirDePartida", "p1");
    expect(conn.stop).toHaveBeenCalled();
  });

  it("con partidaId vacio no crea conexion", () => {
    renderHook(() => useSesionHub("", "tok", {}));
    expect(crearSesionHub).not.toHaveBeenCalled();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd frontend && npx vitest run src/features/partidas/useSesionHub.test.ts`
Expected: FAIL — no existe `./useSesionHub`.

- [ ] **Step 3: Write minimal implementation**

`frontend/src/features/partidas/useSesionHub.ts`:

```ts
import { useEffect, useRef } from "react";
import { crearSesionHub } from "../../api/sesionHub";

export interface SesionHubHandlers {
  onEnLobby?: (payload: { partidaId: string }) => void;
  onIniciada?: (payload: { partidaId: string }) => void;
  onCancelada?: (payload: { partidaId: string; motivo?: string }) => void;
  onJuegoActivado?: (payload: {
    partidaId: string;
    juegoId: string;
    orden: number;
    tipoJuego: string;
  }) => void;
  onFinalizada?: (payload: { partidaId: string }) => void;
}

export function useSesionHub(
  partidaId: string,
  accessToken: string,
  handlers: SesionHubHandlers
): void {
  // Ref para no reconstruir la conexion cuando la pagina pasa handlers inline nuevos en cada render.
  const handlersRef = useRef(handlers);
  handlersRef.current = handlers;

  useEffect(() => {
    if (!partidaId) return;

    const connection = crearSesionHub(accessToken);
    let active = true;

    const suscribir = () => connection.invoke("SuscribirAPartida", partidaId).catch(() => {});

    connection.on("PartidaEnLobby", (p) => handlersRef.current.onEnLobby?.(p));
    connection.on("PartidaIniciada", (p) => handlersRef.current.onIniciada?.(p));
    connection.on("PartidaCancelada", (p) => handlersRef.current.onCancelada?.(p));
    connection.on("JuegoActivado", (p) => handlersRef.current.onJuegoActivado?.(p));
    connection.on("PartidaFinalizada", (p) => handlersRef.current.onFinalizada?.(p));
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
      void connection.stop();
    };
  }, [partidaId, accessToken]);
}
```

- [ ] **Step 4: Run test + tsc to verify they pass**

Run: `cd frontend && npx vitest run src/features/partidas/useSesionHub.test.ts && npx tsc --noEmit`
Expected: PASS (2 tests) + tsc limpio.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/partidas/useSesionHub.ts frontend/src/features/partidas/useSesionHub.test.ts
git commit -m "$(printf 'feat(web): hook useSesionHub para el hub de sesion (bloque 2c1)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>')"
```

---

### Task 4: Página `SesionOperadorPage` (lobby + inicio + shell)

**Files:**
- Create: `frontend/src/features/partidas/SesionOperadorPage.tsx`
- Test: `frontend/src/features/partidas/SesionOperadorPage.test.tsx`

**Interfaces:**
- Consumes: `getEstadoSesion`, `getLobby`, `iniciarPartida`, `OperacionesApiError`, tipos `EstadoSesionDto`/`LobbyDto` (Task 1); `getPartida`, `type ModoInicioPartida` (partidasApi, 2b); `useSesionHub` (Task 3).
- Produces: `export function SesionOperadorPage({ accessToken }: { accessToken: string })`. Ruta `/partidas/:partidaId/sesion` (consumida por Task 5).

**data-testids (fuente para tests):** `sesion-operador` (raíz), `sesion-no-publicada`, `lobby-panel`, `lobby-inscritos`, `btn-iniciar`, `btn-actualizar-lobby`, `inicio-countdown`, `sesion-cancelada`, `sesion-iniciada`, `juego-actual`.

**Orquestación (obligatoria):**
- Al montar: `getEstadoSesion(partidaId)`. `404 (OperacionesApiError)` → `no-publicada`. Si `estado==="Iniciada"` → `iniciada` (usa `estado.juegos`). `Cancelada`/`Terminada` → pantalla terminal. Si `estado==="Lobby"` → además `Promise.all([getLobby, getPartida])` para inscritos + `modoInicio`/`tiempoInicio`.
- Intervalo: mientras la vista sea `lobby`, `setInterval` 5s refetchea `getLobby` y hace merge (`{ ...v, lobby }`); limpiar en cleanup / al salir de lobby. Botón `btn-actualizar-lobby` fuerza el refetch.
- Inicio manual (`btn-iniciar`, disabled mientras postea): `iniciarPartida`; `estado==="Cancelada"` → pantalla `cancelada`; en otro caso `cargar()` de nuevo (trae juegos). `catch` (409 no-en-lobby / modo incompatible) → `cargar()`.
- `useSesionHub`: `onIniciada`→`cargar()`, `onCancelada`→pantalla cancelada+motivo, `onJuegoActivado`→`cargar()`, `onEnLobby`→`cargar()`, `onFinalizada`→terminada.
- Controles de inicio por `modoInicio`: `Manual`→solo `btn-iniciar`; `Automatico`→solo `<Countdown>` (`inicio-countdown`); `ManualYAutomatico`→ambos.

- [ ] **Step 1: Write the failing test**

`frontend/src/features/partidas/SesionOperadorPage.test.tsx`:

```tsx
import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { SesionOperadorPage } from "./SesionOperadorPage";
import {
  getEstadoSesion,
  getLobby,
  iniciarPartida,
  OperacionesApiError,
  type EstadoSesionDto,
  type LobbyDto
} from "../../api/operacionesApi";
import { getPartida, type PartidaDetail } from "../../api/partidasApi";

vi.mock("../../api/operacionesApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../api/operacionesApi")>();
  return { ...actual, getEstadoSesion: vi.fn(), getLobby: vi.fn(), iniciarPartida: vi.fn() };
});
vi.mock("../../api/partidasApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../api/partidasApi")>();
  return { ...actual, getPartida: vi.fn() };
});
vi.mock("./useSesionHub", () => ({ useSesionHub: vi.fn() }));

const estadoLobby: EstadoSesionDto = {
  partidaId: "p1",
  sesionPartidaId: "s1",
  estado: "Lobby",
  modalidad: "Individual",
  juegos: []
};
const lobby: LobbyDto = {
  partidaId: "p1",
  sesionPartidaId: "s1",
  estado: "Lobby",
  modalidad: "Individual",
  minimosParticipacion: 2,
  maximosParticipacion: 10,
  inscritosActivos: 3,
  participantes: [],
  equipos: []
};
const configManual: PartidaDetail = {
  partidaId: "p1",
  nombrePartida: "Copa",
  modalidad: "Individual",
  modoInicioPartida: "Manual",
  tiempoInicio: null,
  minimosParticipacion: 2,
  maximosParticipacion: 10,
  estado: "Lobby",
  juegos: []
};

function renderPage() {
  return render(
    <MemoryRouter initialEntries={["/partidas/p1/sesion"]}>
      <Routes>
        <Route path="/partidas/:partidaId/sesion" element={<SesionOperadorPage accessToken="tok" />} />
        <Route path="/partidas/:partidaId" element={<div>DETALLE</div>} />
      </Routes>
    </MemoryRouter>
  );
}

describe("SesionOperadorPage", () => {
  beforeEach(() => {
    vi.mocked(getEstadoSesion).mockReset();
    vi.mocked(getLobby).mockReset();
    vi.mocked(iniciarPartida).mockReset();
    vi.mocked(getPartida).mockReset();
  });

  it("en Lobby (modo Manual) muestra inscritos y el boton Iniciar", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue(estadoLobby);
    vi.mocked(getLobby).mockResolvedValue(lobby);
    vi.mocked(getPartida).mockResolvedValue(configManual);
    renderPage();

    expect(await screen.findByTestId("lobby-panel")).toBeInTheDocument();
    expect(screen.getByTestId("lobby-inscritos")).toHaveTextContent("3");
    expect(screen.getByTestId("btn-iniciar")).toBeInTheDocument();
    expect(screen.queryByTestId("inicio-countdown")).not.toBeInTheDocument();
  });

  it("inicio manual que devuelve Iniciada muestra el shell con el juego actual", async () => {
    vi.mocked(getEstadoSesion)
      .mockResolvedValueOnce(estadoLobby)
      .mockResolvedValue({
        ...estadoLobby,
        estado: "Iniciada",
        juegos: [
          { juegoId: "j1", orden: 1, tipoJuego: "Trivia", estado: "Activo" },
          { juegoId: "j2", orden: 2, tipoJuego: "BusquedaDelTesoro", estado: "Pendiente" }
        ],
        juegoActualOrden: 1
      });
    vi.mocked(getLobby).mockResolvedValue(lobby);
    vi.mocked(getPartida).mockResolvedValue(configManual);
    vi.mocked(iniciarPartida).mockResolvedValue({ partidaId: "p1", estado: "Iniciada" });
    renderPage();

    await userEvent.click(await screen.findByTestId("btn-iniciar"));

    expect(await screen.findByTestId("sesion-iniciada")).toBeInTheDocument();
    expect(screen.getByTestId("juego-actual")).toHaveTextContent("1");
  });

  it("inicio manual que devuelve Cancelada muestra la pantalla de minimos no alcanzados", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue(estadoLobby);
    vi.mocked(getLobby).mockResolvedValue(lobby);
    vi.mocked(getPartida).mockResolvedValue(configManual);
    vi.mocked(iniciarPartida).mockResolvedValue({ partidaId: "p1", estado: "Cancelada" });
    renderPage();

    await userEvent.click(await screen.findByTestId("btn-iniciar"));
    expect(await screen.findByTestId("sesion-cancelada")).toBeInTheDocument();
  });

  it("modo Automatico muestra countdown y no muestra boton Iniciar", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue(estadoLobby);
    vi.mocked(getLobby).mockResolvedValue(lobby);
    vi.mocked(getPartida).mockResolvedValue({
      ...configManual,
      modoInicioPartida: "Automatico",
      tiempoInicio: new Date(Date.now() + 60000).toISOString()
    });
    renderPage();

    expect(await screen.findByTestId("inicio-countdown")).toBeInTheDocument();
    expect(screen.queryByTestId("btn-iniciar")).not.toBeInTheDocument();
  });

  it("cuando la sesion no existe (404) muestra 'no publicada' con link al detalle", async () => {
    vi.mocked(getEstadoSesion).mockRejectedValue(new OperacionesApiError("no publicada", 404));
    renderPage();

    expect(await screen.findByTestId("sesion-no-publicada")).toBeInTheDocument();
    const link = screen.getByRole("link", { name: /detalle|partida/i });
    expect(link).toHaveAttribute("href", "/partidas/p1");
  });

  it("carga directa con estado Iniciada renderiza el shell sin pasar por lobby", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue({
      ...estadoLobby,
      estado: "Iniciada",
      juegos: [{ juegoId: "j1", orden: 1, tipoJuego: "Trivia", estado: "Activo" }],
      juegoActualOrden: 1
    });
    renderPage();

    expect(await screen.findByTestId("sesion-iniciada")).toBeInTheDocument();
    expect(vi.mocked(getLobby)).not.toHaveBeenCalled();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd frontend && npx vitest run src/features/partidas/SesionOperadorPage.test.tsx`
Expected: FAIL — no existe `./SesionOperadorPage`.

- [ ] **Step 3: Write minimal implementation**

`frontend/src/features/partidas/SesionOperadorPage.tsx`. Estructura obligatoria (rellenar el JSX de las vistas siguiendo las clases CSS de `PartidaDetailPage`/`PartidasListPage`: `page`, `card`, `stack`, `notice error`, `pill pill--live|--lobby|--done`, `pill__dot`, `row-link`, `create-head`, `muted`, `table-wrap`, `secondary-button`, `empty-panel`). Lógica exacta:

```tsx
import { useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { getPartida, type ModoInicioPartida } from "../../api/partidasApi";
import {
  getEstadoSesion,
  getLobby,
  iniciarPartida,
  OperacionesApiError,
  type EstadoSesionDto,
  type LobbyDto
} from "../../api/operacionesApi";
import { useSesionHub, type SesionHubHandlers } from "./useSesionHub";

interface Props {
  accessToken: string;
}

type Vista =
  | { status: "loading" }
  | { status: "no-publicada" }
  | { status: "error"; message: string }
  | {
      status: "lobby";
      estado: EstadoSesionDto;
      lobby: LobbyDto;
      modoInicio: ModoInicioPartida;
      tiempoInicio: string | null;
    }
  | { status: "iniciada"; estado: EstadoSesionDto }
  | { status: "cancelada"; motivo?: string }
  | { status: "terminada" };

export function SesionOperadorPage({ accessToken }: Props) {
  const { partidaId } = useParams<{ partidaId: string }>();
  const [vista, setVista] = useState<Vista>({ status: "loading" });
  const [iniciando, setIniciando] = useState(false);

  const cargar = useCallback(async () => {
    if (!partidaId) {
      setVista({ status: "error", message: "Partida no encontrada" });
      return;
    }
    try {
      const estado = await getEstadoSesion(partidaId, accessToken);
      if (estado.estado === "Iniciada") {
        setVista({ status: "iniciada", estado });
        return;
      }
      if (estado.estado === "Cancelada") {
        setVista({ status: "cancelada" });
        return;
      }
      if (estado.estado === "Terminada") {
        setVista({ status: "terminada" });
        return;
      }
      const [lobby, config] = await Promise.all([
        getLobby(partidaId, accessToken),
        getPartida(partidaId, accessToken)
      ]);
      setVista({
        status: "lobby",
        estado,
        lobby,
        modoInicio: config.modoInicioPartida,
        tiempoInicio: config.tiempoInicio
      });
    } catch (caught) {
      if (caught instanceof OperacionesApiError && caught.statusCode === 404) {
        setVista({ status: "no-publicada" });
        return;
      }
      setVista({
        status: "error",
        message: caught instanceof Error ? caught.message : "Error inesperado al consultar la sesión."
      });
    }
  }, [partidaId, accessToken]);

  useEffect(() => {
    void cargar();
  }, [cargar]);

  // Refetch de inscritos por intervalo (el hub no pushea inscripciones).
  const enLobby = vista.status === "lobby";
  useEffect(() => {
    if (!enLobby || !partidaId) return;
    const id = setInterval(() => {
      getLobby(partidaId, accessToken)
        .then((lobby) => setVista((v) => (v.status === "lobby" ? { ...v, lobby } : v)))
        .catch(() => {});
    }, 5000);
    return () => clearInterval(id);
  }, [enLobby, partidaId, accessToken]);

  const handlers: SesionHubHandlers = {
    onEnLobby: () => void cargar(),
    onIniciada: () => void cargar(),
    onCancelada: (p) => setVista({ status: "cancelada", motivo: p.motivo }),
    onJuegoActivado: () => void cargar(),
    onFinalizada: () => setVista({ status: "terminada" })
  };
  useSesionHub(partidaId ?? "", accessToken, handlers);

  async function onIniciar() {
    if (!partidaId) return;
    setIniciando(true);
    try {
      const r = await iniciarPartida(partidaId, accessToken);
      if (r.estado === "Cancelada") {
        setVista({ status: "cancelada" });
      } else {
        await cargar();
      }
    } catch {
      await cargar();
    } finally {
      setIniciando(false);
    }
  }

  return (
    <div className="page" data-testid="sesion-operador">
      {/* loading / no-publicada / error / lobby / iniciada / cancelada / terminada */}
      {/* Ver vistas abajo. */}
      {renderVista(vista, { partidaId: partidaId ?? "", iniciando, onIniciar, onActualizar: () => void cargar() })}
    </div>
  );
}
```

Vistas (helper `renderVista` o inline). Requisitos por estado:
- `loading`: `<p className="muted">Cargando sesión…</p>`.
- `no-publicada`: contenedor `data-testid="sesion-no-publicada"`, mensaje "La partida no está publicada." + `<Link to={\`/partidas/${partidaId}\`} className="row-link">Ir al detalle para publicar</Link>`.
- `error`: `notice error` con el message.
- `lobby`: contenedor `data-testid="lobby-panel"`. Muestra `modalidad`, `Min ${min} · Max ${max}`, e inscritos en un elemento `data-testid="lobby-inscritos"` con el número `lobby.inscritosActivos` (texto puede ser `${inscritos} / min ${min}`). Botón `data-testid="btn-actualizar-lobby"` (`secondary-button`) → `onActualizar`. Si `modalidad==="Equipo"` y `lobby.equipos.length`, tabla con `equipoId`/`convocados`/`aceptados`. Controles de inicio:
  - `modoInicio==="Manual"` o `"ManualYAutomatico"`: `<button data-testid="btn-iniciar" disabled={iniciando} onClick={onIniciar}>Iniciar ahora</button>`.
  - `modoInicio==="Automatico"` o `"ManualYAutomatico"`: `<Countdown target={tiempoInicio} />` (si `tiempoInicio` no es null; si es null, texto "Inicio automático pendiente de configuración").
- `iniciada`: contenedor `data-testid="sesion-iniciada"`. Lista de `estado.juegos` ordenada por `orden`; el juego cuyo `orden===estado.juegoActualOrden` marcado con `data-testid="juego-actual"` (y clase resaltada, p.ej. `pill--live`) mostrando su `orden` y `tipoJuego`. Nota `muted`: "El runtime del juego (preguntas/etapas) llega en 2c-2/2c-3.".
- `cancelada`: contenedor `data-testid="sesion-cancelada"`, mensaje "La partida fue cancelada (mínimos de participación no alcanzados)." + `motivo` si viene.
- `terminada`: mensaje neutral "La partida finalizó." (el consolidado es 2c-4).

Componente `Countdown`:

```tsx
function Countdown({ target }: { target: string }) {
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, []);
  const remaining = Math.max(0, Math.floor((new Date(target).getTime() - now) / 1000));
  const mm = String(Math.floor(remaining / 60)).padStart(2, "0");
  const ss = String(remaining % 60).padStart(2, "0");
  return (
    <span data-testid="inicio-countdown">{remaining > 0 ? `${mm}:${ss}` : "Iniciando…"}</span>
  );
}
```

- [ ] **Step 4: Run test + tsc to verify they pass**

Run: `cd frontend && npx vitest run src/features/partidas/SesionOperadorPage.test.tsx && npx tsc --noEmit`
Expected: PASS (6 tests) + tsc limpio. (Si `@testing-library/user-event` no está instalado, usar `fireEvent.click` de `@testing-library/react` en su lugar — verificar con `grep user-event frontend/package.json`.)

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/partidas/SesionOperadorPage.tsx frontend/src/features/partidas/SesionOperadorPage.test.tsx
git commit -m "$(printf 'feat(web): consola de sesion operador lobby+inicio+shell (bloque 2c1)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>')"
```

---

### Task 5: Acción publicar en detalle + ruta + nav

**Files:**
- Modify: `frontend/src/features/partidas/PartidaDetailPage.tsx`
- Modify: `frontend/src/app/App.tsx`
- Modify: `frontend/src/shell/navConfig.tsx`
- Test: `frontend/src/features/partidas/PartidaDetailPage.test.tsx` (añadir casos), `frontend/src/shell/navConfig.test.tsx` (si existe, añadir caso; si no, crear test mínimo)

**Interfaces:**
- Consumes: `publicarPartida`, `OperacionesApiError` (Task 1); `SesionOperadorPage` (Task 4).
- Produces: ruta `partidas/:partidaId/sesion`; botón `data-testid="btn-publicar-operar"` en el detalle; `titleForPath` devuelve `"Sesión en vivo"` para paths que terminan en `/sesion`.

- [ ] **Step 1: Write the failing tests**

Añadir a `frontend/src/features/partidas/PartidaDetailPage.test.tsx` (el archivo ya mockea `partidasApi`; extender el mock para incluir `getPartida` como está y añadir mock de `operacionesApi`):

```tsx
// arriba, junto a los otros imports/mocks:
import { publicarPartida, OperacionesApiError } from "../../api/operacionesApi";
vi.mock("../../api/operacionesApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../api/operacionesApi")>();
  return { ...actual, publicarPartida: vi.fn() };
});

// renderPage debe montar tambien la ruta destino para verificar navegacion:
function renderPageConSesion(partidaId = "p1") {
  return render(
    <MemoryRouter initialEntries={[`/partidas/${partidaId}`]}>
      <Routes>
        <Route path="/partidas/:partidaId" element={<PartidaDetailPage accessToken="token" />} />
        <Route path="/partidas/:partidaId/sesion" element={<div>CONSOLA SESION</div>} />
      </Routes>
    </MemoryRouter>
  );
}

// nuevos casos:
it("publicar y operar publica y navega a la consola de sesion", async () => {
  getPartidaMock.mockResolvedValueOnce(detail);
  vi.mocked(publicarPartida).mockResolvedValueOnce({
    partidaId: "p1",
    sesionPartidaId: "s1",
    estado: "Lobby",
    modalidad: "Individual",
    minimosParticipacion: 1,
    maximosParticipacion: 10,
    inscritosActivos: 0,
    participantes: [],
    equipos: []
  });
  renderPageConSesion();
  await screen.findByTestId("detalle-partida");
  await userEvent.click(screen.getByTestId("btn-publicar-operar"));
  expect(vi.mocked(publicarPartida)).toHaveBeenCalledWith("p1", "token");
  expect(await screen.findByText("CONSOLA SESION")).toBeInTheDocument();
});

it("si la partida ya estaba publicada (409) igual navega a la consola", async () => {
  getPartidaMock.mockResolvedValueOnce(detail);
  vi.mocked(publicarPartida).mockRejectedValueOnce(new OperacionesApiError("ya publicada", 409));
  renderPageConSesion();
  await screen.findByTestId("detalle-partida");
  await userEvent.click(screen.getByTestId("btn-publicar-operar"));
  expect(await screen.findByText("CONSOLA SESION")).toBeInTheDocument();
});
```

Para `navConfig`: si `frontend/src/shell/navConfig.test.tsx` no existe, crearlo:

```tsx
import { describe, expect, it } from "vitest";
import { titleForPath } from "./navConfig";

describe("titleForPath", () => {
  it("devuelve 'Sesión en vivo' para la ruta de sesion", () => {
    expect(titleForPath("/partidas/p1/sesion")).toBe("Sesión en vivo");
  });
  it("devuelve 'Detalle de partida' para el detalle", () => {
    expect(titleForPath("/partidas/p1")).toBe("Detalle de partida");
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd frontend && npx vitest run src/features/partidas/PartidaDetailPage.test.tsx src/shell/navConfig.test.tsx`
Expected: FAIL — no existe `btn-publicar-operar`; `titleForPath("/partidas/p1/sesion")` devuelve "Detalle de partida".

- [ ] **Step 3: Implement**

**3a. `navConfig.tsx`** — en `titleForPath`, antes del branch `if (pathname.startsWith("/partidas/"))`:

```tsx
  if (pathname.endsWith("/sesion")) {
    return "Sesión en vivo";
  }
```

**3b. `PartidaDetailPage.tsx`** — imports:

```tsx
import { Link, useNavigate, useParams } from "react-router-dom";
import { useState } from "react"; // ya importa useEffect/useState; añadir si falta
import { publicarPartida, OperacionesApiError } from "../../api/operacionesApi";
```

Pasar `accessToken` a `PartidaDetailContent` y renderizar la acción. Cambiar la firma y la llamada:

```tsx
{state.status === "ready" ? (
  <PartidaDetailContent partida={state.partida} accessToken={accessToken} />
) : null}
```

En `PartidaDetailContent`:

```tsx
function PartidaDetailContent({ partida, accessToken }: { partida: PartidaDetail; accessToken: string }) {
  const navigate = useNavigate();
  const [publicando, setPublicando] = useState(false);
  const [pubError, setPubError] = useState<string | null>(null);
  const pillEstado = estadoPill(partida.estado);
  const juegos = [...partida.juegos].sort((a, b) => a.orden - b.orden);

  async function onPublicar() {
    setPublicando(true);
    setPubError(null);
    try {
      await publicarPartida(partida.partidaId, accessToken);
      navigate(`/partidas/${partida.partidaId}/sesion`);
    } catch (caught) {
      if (caught instanceof OperacionesApiError && caught.statusCode === 409) {
        navigate(`/partidas/${partida.partidaId}/sesion`);
        return;
      }
      setPubError(caught instanceof Error ? caught.message : "No se pudo publicar la partida.");
    } finally {
      setPublicando(false);
    }
  }

  return (
    <div className="card stack">
      <header className="create-head">
        <div>
          <h1>{partida.nombrePartida}</h1>
          <div className="compact-actions">
            <Pill cls="pill--done" label={partida.modalidad} />
            <Pill cls="pill--done" label={partida.modoInicioPartida} />
            <Pill cls={pillEstado.cls} label={pillEstado.label} />
            <Pill
              cls="pill--done"
              label={`Min ${partida.minimosParticipacion} · Max ${partida.maximosParticipacion}`}
            />
          </div>
        </div>
        <button
          type="button"
          data-testid="btn-publicar-operar"
          disabled={publicando}
          onClick={() => void onPublicar()}
        >
          Publicar y operar
        </button>
      </header>

      {pubError ? (
        <div className="notice error" role="alert">
          {pubError}
        </div>
      ) : null}

      <div className="question-list">
        {juegos.map((juego) => (
          <JuegoCard key={juego.juegoId} juego={juego} />
        ))}
      </div>
    </div>
  );
}
```

**3c. `App.tsx`** — import y ruta:

```tsx
import { SesionOperadorPage } from "../features/partidas/SesionOperadorPage";
```

Añadir tras la ruta `partidas/:partidaId`:

```tsx
{
  path: "partidas/:partidaId/sesion",
  element: (
    <RequireRole roles={roles} need="Operador" landing={landing}>
      <SesionOperadorPage accessToken={token} />
    </RequireRole>
  )
},
```

- [ ] **Step 4: Run tests + tsc + suite completa**

Run: `cd frontend && npx vitest run && npx tsc --noEmit`
Expected: PASS toda la suite (incluye los casos nuevos) + tsc limpio. Si algún test previo de `App.test.tsx` rompe por el import nuevo, arreglarlo mínimamente (no cambiar comportamiento existente).

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/partidas/PartidaDetailPage.tsx frontend/src/app/App.tsx frontend/src/shell/navConfig.tsx frontend/src/features/partidas/PartidaDetailPage.test.tsx frontend/src/shell/navConfig.test.tsx
git commit -m "$(printf 'feat(web): accion publicar+operar, ruta /sesion y titulo nav (bloque 2c1)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>')"
```

---

### Task 6: Gate E2E vivo + traceability (controller)

**Files:**
- Modify: `docs/04-sdd/traceability-matrix.md` (fila Bloque 2c-1)
- Modify: `GUIA-LEVANTAMIENTO.md` (nota: la consola de sesión requiere `services/operaciones-sesion` + `services/partidas` + gateway arriba)
- Scratch (no commit): script E2E + smoke SignalR en el scratchpad.

**Este task es un gate operado por el controlador, no código de app.** No re-despachar a implementer haiku para el E2E; el controlador levanta el stack y ejecuta.

- [ ] **Step 1: Levantar stack**

```bash
docker compose -f infra/docker-compose.yml up -d postgres rabbitmq keycloak
# crear DBs si es primera vez (ver CLAUDE.md)
./services/partidas/run-local.sh &
./services/operaciones-sesion/run-local.sh &
./gateway/run-local.sh &
```
Verificar `/health` de cada servicio y del gateway.

- [ ] **Step 2: E2E HTTP (patrón 2b: authorization-code+PKCE via curl+python, usuario operador/operador)**

Obtener token de operador contra `umbral-web` (público PKCE, sin direct-grant). Luego, contra gateway `:5080`:
1. `POST /partidas` (crear partida modo `Manual`, min 2) → 201, guardar `partidaId`.
2. `POST /partidas/{id}/juegos/trivia` (1 pregunta) → 201.
3. `POST /operaciones-sesion/partidas/{id}/publicacion` → **201**, body estado `Lobby`.
4. `GET /operaciones-sesion/partidas/{id}/lobby` → **200**, `inscritosActivos: 0`.
5. `POST /operaciones-sesion/partidas/{id}/inicio` (sin inscritos, min 2) → **200 estado=Cancelada** (mínimos no alcanzados — camino limpio single-actor).
6. Con token de `participante`: `POST /operaciones-sesion/partidas/{id}/publicacion` → **403** (policy `GestionarPartidas`).

Registrar los códigos reales observados.

- [ ] **Step 3: Smoke SignalR (node, usando el @microsoft/signalr ya instalado en frontend)**

Script `scratchpad/sesion-smoke.mjs` que importe `@microsoft/signalr` desde `frontend/node_modules`, conecte al hub `:5080/operaciones-sesion/hubs/sesion` con `accessTokenFactory` = token operador, `SuscribirAPartida(partidaId)` de una partida recién publicada, registre `connection.on("PartidaCancelada", ...)` y `on("PartidaIniciada", ...)`, luego dispare `POST /inicio` (min no alcanzado) y **espere recibir `PartidaCancelada` en la conexión** dentro de ~5s. Si el WS handshake vía gateway funciona y llega el mensaje → smoke PASS. Si el entorno no permite WS en la sesión, documentarlo como diferido a verificación manual en navegador (no bloquea el gate HTTP).

- [ ] **Step 4: Traceability + GUIA**

Añadir fila Bloque 2c-1 a `docs/04-sdd/traceability-matrix.md` con el mismo formato de 7 columnas que la fila 2b (HU lado operador publicación/inicio, artefactos web, tests, evidencia E2E gateway). **Verificar cada hash de commit con `git cat-file -t <hash>` antes de escribirlo** (lección T8 2b: haiku fabrica hashes). Nota en `GUIA-LEVANTAMIENTO.md` sobre los servicios necesarios para la consola.

- [ ] **Step 5: Commit**

```bash
git add docs/04-sdd/traceability-matrix.md GUIA-LEVANTAMIENTO.md
git commit -m "$(printf 'docs(bloque2c1): traceability consola de sesion + nota GUIA operaciones\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>')"
```

---

## Self-Review (hecho por el autor del plan)

**Spec coverage:**
- Cliente HTTP operacionesApi (spec §Componentes.1) → Task 1. ✓
- Cliente SignalR + install (spec §2) → Task 2. ✓
- Hook useSesionHub (spec §3) → Task 3. ✓
- Página consola: carga/estado, lobby+inscritos+intervalo, inicio manual/auto/countdown, resultado Iniciada/Cancelada, shell post-inicio, terminales, SignalR (spec §4) → Task 4. ✓
- Ediciones detalle publicar + ruta + nav (spec §5) → Task 5. ✓
- Testing unit (spec §Testing) → Tasks 1-5; E2E gate + smoke SignalR + policy 403 (spec §Testing) → Task 6. ✓
- Legacy pages intactas (spec §Fuera de alcance) → ningún task las toca. ✓

**Placeholder scan:** sin TBD/TODO. Las vistas JSX de Task 4 se describen con requisitos concretos + testids + clases CSS reales (no "add appropriate UI"); la lógica (fetch, intervalo, countdown, inicio, hub) va verbatim.

**Type consistency:** `EstadoSesion`/`Modalidad`/`LobbyDto`/`EstadoSesionDto`/`InicioPartidaResponse` (Task 1) se consumen con esos nombres en Task 4; `SesionHubHandlers` (Task 3) idem; `publicarPartida(partidaId, accessToken)` firma consistente entre Task 1, Task 4-page y Task 5-detalle; `crearSesionHub` (Task 2) consumido por Task 3. `ModoInicioPartida` importado de partidasApi (2b, ya existe). ✓
```
