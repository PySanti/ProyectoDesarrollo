# Bloque 2f — UI Puntuaciones web Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** UI web (operador/admin) para el historial cronológico de partida (HU-43) y el rendimiento histórico de equipo (HU-49), consumiendo los endpoints SP-4 de Puntuaciones ya vigentes.

**Architecture:** Espejo del patrón web existente: `puntuacionesApi.ts` gana 2 funciones tipadas que lanzan `PuntuacionesApiError` → 2 páginas nuevas bajo `features/partidas/` y `features/puntuaciones/` con el markup del design-system (`card stack`, `table-wrap`, `muted`, `notice error`) → rutas en `App.tsx` con `RequireRole need="Operador"` + área de nav "Puntuaciones". Sin cambios backend/contrato.

**Tech Stack:** React 18 + Vite + TypeScript, react-router-dom, vitest + testing-library.

## Global Constraints

- Spec: `docs/superpowers/specs/2026-07-10-bloque2f-web-ui-puntuaciones-design.md`.
- Solo `frontend/` (+docs en T4). PROHIBIDO tocar backend, gateway, mobile o `contracts/`.
- Patrón api web: funciones `async (…, accessToken, fetchImpl: typeof fetch = fetch)` que usan `resolveBaseUrl()` interno y lanzan `PuntuacionesApiError(message, statusCode)` en `!response.ok` (ver funciones existentes del archivo).
- Design system: reutilizar clases existentes (`page`, `card`, `stack`, `muted`, `notice error`, `table-wrap`, `row-link`, `compact-actions`) — NO inventar clases ni CSS nuevo.
- Los 17 tipos de evento canónicos (de `contracts/events/operaciones-sesion-events.md`): `PartidaPublicadaEnLobby`, `PartidaIniciada`, `PartidaCancelada`, `PartidaFinalizada`, `JuegoActivado`, `PreguntaTriviaActivada`, `RespuestaTriviaValidada`, `PuntajeTriviaIncrementado`, `PreguntaTriviaCerrada`, `EtapaBDTActivada`, `TesoroQRValidado`, `EtapaBDTGanada`, `EtapaBDTCerrada`, `PistaEnviada`, `ConvocatoriaCreada`, `ConvocatoriaRespondida`, `UbicacionActualizada`.
- Gates por tarea: `cd frontend && npm test` (baseline pre-T1: **138 tests**) y `npx tsc -b` (limpio; borrar artefactos `tsconfig.tsbuildinfo`/`dist` si los genera).
- Commits con trailer: `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- PROHIBIDO a implementers: `git stash/reset/checkout/restore/clean`. Solo `git add <paths exactos>` + `git commit`.

---

### Task 1: `puntuacionesApi.ts` — `getHistorialPartida` + `getRendimientoEquipo` + tests

**Files:**
- Modify: `frontend/src/api/puntuacionesApi.ts` (append al final; NO tocar lo existente)
- Test: `frontend/src/api/puntuacionesApi.test.ts` (append; NO tocar los existentes)

**Interfaces:**
- Consumes: `resolveBaseUrl()` y `PuntuacionesApiError` ya presentes en el archivo.
- Produces (T2/T3 los consumen):
  - `getHistorialPartida(partidaId: string, accessToken: string, opts?: HistorialQueryOpts, fetchImpl?: typeof fetch): Promise<HistorialPartidaDto>` con `HistorialQueryOpts {limit?: number; offset?: number; tipo?: string}`, `HistorialPartidaDto {partidaId: string; total: number; entradas: EventoHistorialDto[]}`, `EventoHistorialDto {occurredAt: string; tipoEvento: string; juegoId: string | null; participanteId: string | null; equipoId: string | null; detalle: unknown}`.
  - `getRendimientoEquipo(equipoId: string, accessToken: string, fetchImpl?: typeof fetch): Promise<RendimientoEquipoDto>` con `RendimientoEquipoDto {equipoId: string; partidas: RendimientoPartidaDto[]}`, `RendimientoPartidaDto {partidaId: string; fechaFin: string; posicion: number; gano: boolean}`.

- [ ] **Step 1: Tests que fallan** — append a `frontend/src/api/puntuacionesApi.test.ts` (dentro del `describe` existente) y añadir `getHistorialPartida, getRendimientoEquipo` al import:

```ts
  it("getHistorialPartida arma query solo con los opts presentes", async () => {
    const f = okJson({ partidaId: "p1", total: 2, entradas: [] });
    await getHistorialPartida("p1", "tok", { limit: 50, tipo: "PistaEnviada" }, f);
    expect(f.mock.calls[0][0]).toBe(
      "https://gw.example.test/puntuaciones/partidas/p1/historial?limit=50&tipo=PistaEnviada"
    );
    await getHistorialPartida("p1", "tok", {}, f);
    expect(f.mock.calls[1][0]).toBe("https://gw.example.test/puntuaciones/partidas/p1/historial");
    expect((f.mock.calls[0][1].headers as Record<string, string>).Authorization).toBe("Bearer tok");
  });

  it("getHistorialPartida 403 de participante lanza PuntuacionesApiError", async () => {
    const f = okJson({ message: "solo operador/administrador" }, 403);
    await expect(getHistorialPartida("p1", "tok", {}, f)).rejects.toMatchObject({
      statusCode: 403
    });
  });

  it("getRendimientoEquipo hace GET autenticado y devuelve partidas", async () => {
    const f = okJson({
      equipoId: "e1",
      partidas: [{ partidaId: "p1", fechaFin: "2026-07-10T12:00:00Z", posicion: 1, gano: true }]
    });
    const r = await getRendimientoEquipo("e1", "tok", f);
    expect(r.partidas[0].gano).toBe(true);
    expect(f.mock.calls[0][0]).toBe("https://gw.example.test/puntuaciones/equipos/e1/rendimiento");
  });
```

- [ ] **Step 2: Verificar que fallan** — Run: `cd frontend && npx vitest run src/api/puntuacionesApi.test.ts`. Expected: FAIL (`getHistorialPartida` no exportado).

- [ ] **Step 3: Implementar** — append a `frontend/src/api/puntuacionesApi.ts`:

```ts
export interface EventoHistorialDto {
  occurredAt: string;
  tipoEvento: string;
  juegoId: string | null;
  participanteId: string | null;
  equipoId: string | null;
  detalle: unknown;
}

export interface HistorialPartidaDto {
  partidaId: string;
  total: number;
  entradas: EventoHistorialDto[];
}

export interface HistorialQueryOpts {
  limit?: number;
  offset?: number;
  tipo?: string;
}

export async function getHistorialPartida(
  partidaId: string,
  accessToken: string,
  opts: HistorialQueryOpts = {},
  fetchImpl: typeof fetch = fetch
): Promise<HistorialPartidaDto> {
  const params = new URLSearchParams();
  if (opts.limit != null) params.set("limit", String(opts.limit));
  if (opts.offset != null) params.set("offset", String(opts.offset));
  if (opts.tipo) params.set("tipo", opts.tipo);
  const query = params.toString();
  const response = await fetchImpl(
    `${resolveBaseUrl()}/puntuaciones/partidas/${partidaId}/historial${query ? `?${query}` : ""}`,
    { method: "GET", headers: { Authorization: `Bearer ${accessToken}` } }
  );
  const body = (await response.json().catch(() => ({}))) as HistorialPartidaDto & {
    message?: string;
  };
  if (!response.ok) {
    const message = body.message ?? `Puntuaciones API error. Status=${response.status}`;
    throw new PuntuacionesApiError(message, response.status);
  }
  return body;
}

export interface RendimientoPartidaDto {
  partidaId: string;
  fechaFin: string;
  posicion: number;
  gano: boolean;
}

export interface RendimientoEquipoDto {
  equipoId: string;
  partidas: RendimientoPartidaDto[];
}

export async function getRendimientoEquipo(
  equipoId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<RendimientoEquipoDto> {
  const response = await fetchImpl(
    `${resolveBaseUrl()}/puntuaciones/equipos/${equipoId}/rendimiento`,
    { method: "GET", headers: { Authorization: `Bearer ${accessToken}` } }
  );
  const body = (await response.json().catch(() => ({}))) as RendimientoEquipoDto & {
    message?: string;
  };
  if (!response.ok) {
    const message = body.message ?? `Puntuaciones API error. Status=${response.status}`;
    throw new PuntuacionesApiError(message, response.status);
  }
  return body;
}
```

- [ ] **Step 4: Verificar verde** — Run: `cd frontend && npx vitest run src/api/puntuacionesApi.test.ts` (todos PASS), `npm test` (141), `npx tsc -b` (limpio).

- [ ] **Step 5: Commit**

```bash
git add frontend/src/api/puntuacionesApi.ts frontend/src/api/puntuacionesApi.test.ts
git commit -m "feat(web): puntuacionesApi historial de partida y rendimiento de equipo (bloque 2f)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 2: `HistorialPartidaPage` + ruta + enlaces + tests

**Files:**
- Create: `frontend/src/features/partidas/HistorialPartidaPage.tsx`
- Create: `frontend/src/features/partidas/HistorialPartidaPage.test.tsx`
- Modify: `frontend/src/app/App.tsx` (ruta nueva)
- Modify: `frontend/src/shell/navConfig.tsx` (`titleForPath`)
- Modify: `frontend/src/features/partidas/PartidaDetailPage.tsx` (Link)
- Modify: `frontend/src/features/partidas/SesionOperadorPage.tsx` (Link en vista terminada)

**Interfaces:**
- Consumes: `getHistorialPartida`/`HistorialPartidaDto`/`EventoHistorialDto`/`PuntuacionesApiError` (T1).
- Produces: ruta `partidas/:partidaId/historial`; constante exportada `TIPOS_EVENTO: string[]` (los 17 tipos, por si otra vista la necesita).

- [ ] **Step 1: Crear la página** — `frontend/src/features/partidas/HistorialPartidaPage.tsx`:

```tsx
// Historial cronológico de la partida (HU-43): eventos proyectados por Puntuaciones,
// paginado limit/offset con filtro por tipo. Solo Operador/Administrador (403 backend).
import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import {
  getHistorialPartida,
  PuntuacionesApiError,
  type HistorialPartidaDto
} from "../../api/puntuacionesApi";

export const TIPOS_EVENTO = [
  "PartidaPublicadaEnLobby",
  "PartidaIniciada",
  "PartidaCancelada",
  "PartidaFinalizada",
  "JuegoActivado",
  "PreguntaTriviaActivada",
  "RespuestaTriviaValidada",
  "PuntajeTriviaIncrementado",
  "PreguntaTriviaCerrada",
  "EtapaBDTActivada",
  "TesoroQRValidado",
  "EtapaBDTGanada",
  "EtapaBDTCerrada",
  "PistaEnviada",
  "ConvocatoriaCreada",
  "ConvocatoriaRespondida",
  "UbicacionActualizada"
];

const LIMIT = 100;

type Estado =
  | { status: "cargando" }
  | { status: "ok"; historial: HistorialPartidaDto }
  | { status: "error"; message: string };

const guidCorto = (v: string | null) => (v ? v.slice(0, 8) : "—");

export function HistorialPartidaPage({ accessToken }: { accessToken: string }) {
  const { partidaId } = useParams<{ partidaId: string }>();
  const [estado, setEstado] = useState<Estado>({ status: "cargando" });
  const [tipo, setTipo] = useState("");
  const [offset, setOffset] = useState(0);

  useEffect(() => {
    if (!partidaId) return;
    let active = true;
    setEstado({ status: "cargando" });
    getHistorialPartida(partidaId, accessToken, {
      limit: LIMIT,
      offset,
      ...(tipo ? { tipo } : {})
    })
      .then((historial) => {
        if (active) setEstado({ status: "ok", historial });
      })
      .catch((caught) => {
        if (!active) return;
        const message =
          caught instanceof PuntuacionesApiError && caught.statusCode === 404
            ? "La partida no existe en la proyección de Puntuaciones."
            : caught instanceof Error
              ? caught.message
              : "Error inesperado al consultar el historial.";
        setEstado({ status: "error", message });
      });
    return () => {
      active = false;
    };
  }, [partidaId, accessToken, tipo, offset]);

  const total = estado.status === "ok" ? estado.historial.total : 0;
  const desde = total === 0 ? 0 : offset + 1;
  const hasta = estado.status === "ok" ? offset + estado.historial.entradas.length : 0;

  return (
    <div className="page" data-testid="historial-partida">
      <div className="card stack">
        <h1>Historial de la partida</h1>
        <div className="compact-actions">
          <label>
            Tipo de evento{" "}
            <select
              value={tipo}
              aria-label="Filtrar por tipo de evento"
              onChange={(e) => {
                setTipo(e.target.value);
                setOffset(0);
              }}
            >
              <option value="">Todos</option>
              {TIPOS_EVENTO.map((t) => (
                <option key={t} value={t}>
                  {t}
                </option>
              ))}
            </select>
          </label>
          <Link to={`/partidas/${partidaId}`} className="row-link">
            Volver a la partida
          </Link>
        </div>

        {estado.status === "cargando" ? <p className="muted">Cargando historial…</p> : null}
        {estado.status === "error" ? (
          <div className="notice error" role="alert">
            {estado.message}
          </div>
        ) : null}

        {estado.status === "ok" ? (
          estado.historial.entradas.length === 0 ? (
            <p className="muted">Sin eventos registrados.</p>
          ) : (
            <>
              <div className="table-wrap">
                <table aria-label="Historial de eventos" data-testid="tabla-historial">
                  <thead>
                    <tr>
                      <th scope="col">Momento</th>
                      <th scope="col">Evento</th>
                      <th scope="col">Juego</th>
                      <th scope="col">Participante</th>
                      <th scope="col">Equipo</th>
                      <th scope="col">Detalle</th>
                    </tr>
                  </thead>
                  <tbody>
                    {estado.historial.entradas.map((e, i) => (
                      <tr key={`${e.occurredAt}-${i}`}>
                        <td>{new Date(e.occurredAt).toLocaleString()}</td>
                        <td>{e.tipoEvento}</td>
                        <td>{guidCorto(e.juegoId)}</td>
                        <td>{guidCorto(e.participanteId)}</td>
                        <td>{guidCorto(e.equipoId)}</td>
                        <td className="muted">{JSON.stringify(e.detalle)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <div className="compact-actions">
                <button type="button" disabled={offset === 0} onClick={() => setOffset(Math.max(0, offset - LIMIT))}>
                  Anterior
                </button>
                <span className="muted">
                  {desde}–{hasta} de {total}
                </span>
                <button
                  type="button"
                  disabled={offset + LIMIT >= total}
                  onClick={() => setOffset(offset + LIMIT)}
                >
                  Siguiente
                </button>
              </div>
            </>
          )
        ) : null}
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Tests** — `frontend/src/features/partidas/HistorialPartidaPage.test.tsx`:

```tsx
import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { HistorialPartidaPage } from "./HistorialPartidaPage";
import * as puntuacionesApi from "../../api/puntuacionesApi";
import { PuntuacionesApiError } from "../../api/puntuacionesApi";

const historial = {
  partidaId: "p1",
  total: 150,
  entradas: [
    {
      occurredAt: "2026-07-10T12:00:00Z",
      tipoEvento: "EtapaBDTGanada",
      juegoId: "abcdef12-0000-0000-0000-000000000000",
      participanteId: "11223344-0000-0000-0000-000000000000",
      equipoId: null,
      detalle: { puntaje: 50 }
    }
  ]
};

function renderPage() {
  return render(
    <MemoryRouter initialEntries={["/partidas/p1/historial"]}>
      <Routes>
        <Route path="/partidas/:partidaId/historial" element={<HistorialPartidaPage accessToken="tok" />} />
      </Routes>
    </MemoryRouter>
  );
}

afterEach(() => vi.restoreAllMocks());

describe("HistorialPartidaPage", () => {
  it("muestra la tabla con eventos y el rango de paginación", async () => {
    vi.spyOn(puntuacionesApi, "getHistorialPartida").mockResolvedValue(historial);
    renderPage();
    expect(await screen.findByTestId("tabla-historial")).toBeInTheDocument();
    expect(screen.getByText("EtapaBDTGanada")).toBeInTheDocument();
    expect(screen.getByText("abcdef12")).toBeInTheDocument();
    expect(screen.getByText(/1–1 de 150/)).toBeInTheDocument();
    expect(screen.getByText('{"puntaje":50}')).toBeInTheDocument();
  });

  it("cambiar el filtro de tipo resetea offset y re-consulta con tipo", async () => {
    const spy = vi.spyOn(puntuacionesApi, "getHistorialPartida").mockResolvedValue(historial);
    renderPage();
    await screen.findByTestId("tabla-historial");
    await userEvent.click(screen.getByText("Siguiente"));
    await userEvent.selectOptions(
      screen.getByLabelText("Filtrar por tipo de evento"),
      "PistaEnviada"
    );
    const ultima = spy.mock.calls[spy.mock.calls.length - 1];
    expect(ultima[2]).toMatchObject({ offset: 0, tipo: "PistaEnviada" });
  });

  it("404 muestra el mensaje de proyección", async () => {
    vi.spyOn(puntuacionesApi, "getHistorialPartida").mockRejectedValue(
      new PuntuacionesApiError("no existe", 404)
    );
    renderPage();
    expect(
      await screen.findByText("La partida no existe en la proyección de Puntuaciones.")
    ).toBeInTheDocument();
  });

  it("200 sin eventos muestra vacío", async () => {
    vi.spyOn(puntuacionesApi, "getHistorialPartida").mockResolvedValue({
      partidaId: "p1",
      total: 0,
      entradas: []
    });
    renderPage();
    expect(await screen.findByText("Sin eventos registrados.")).toBeInTheDocument();
  });
});
```

- [ ] **Step 3: Verificar que fallan → implementación ya escrita en Step 1** — Run: `cd frontend && npx vitest run src/features/partidas/HistorialPartidaPage.test.tsx`. Expected: PASS 4/4 (página creada en Step 1; si algo falla, corregir la página, no los tests).

- [ ] **Step 4: Ruta + título + enlaces** —
En `frontend/src/app/App.tsx`: importar `HistorialPartidaPage` junto a los imports de partidas y añadir tras la ruta `partidas/:partidaId/sesion`:

```tsx
          {
            path: "partidas/:partidaId/historial",
            element: (
              <RequireRole roles={roles} need="Operador" landing={landing}>
                <HistorialPartidaPage accessToken={token} />
              </RequireRole>
            )
          },
```

En `frontend/src/shell/navConfig.tsx`, en `titleForPath`, ANTES del `if (pathname.startsWith("/partidas/"))`:

```ts
  if (pathname.endsWith("/historial")) {
    return "Historial de la partida";
  }
```

En `frontend/src/features/partidas/PartidaDetailPage.tsx`, dentro de `PartidaDetailContent`, inmediatamente después del cierre del `<div className="compact-actions">…</div>` de las pills (dentro del `<header className="create-head">`), añadir:

```tsx
          <Link to={`/partidas/${partida.partidaId}/historial`} className="row-link">
            Historial de eventos
          </Link>
```

En `frontend/src/features/partidas/SesionOperadorPage.tsx`, caso `"terminada"` del render (hoy `return <ConsolidadoPanel …/>`), reemplazar por (añadiendo `Link` al import de react-router-dom si falta):

```tsx
    case "terminada":
      return (
        <div className="stack">
          <ConsolidadoPanel partidaId={ctx.partidaId} accessToken={ctx.accessToken} />
          <Link to={`/partidas/${ctx.partidaId}/historial`} className="row-link">
            Ver historial de la partida
          </Link>
        </div>
      );
```

- [ ] **Step 5: Verificar gates** — Run: `cd frontend && npm test` (145 = 141 + 4) y `npx tsc -b` (limpio).

- [ ] **Step 6: Commit**

```bash
git add frontend/src/features/partidas/HistorialPartidaPage.tsx frontend/src/features/partidas/HistorialPartidaPage.test.tsx frontend/src/app/App.tsx frontend/src/shell/navConfig.tsx frontend/src/features/partidas/PartidaDetailPage.tsx frontend/src/features/partidas/SesionOperadorPage.tsx
git commit -m "feat(web): historial cronologico de partida HU-43 (bloque 2f)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 3: `RendimientoEquipoPage` + nav + ruta + tests

**Files:**
- Create: `frontend/src/features/puntuaciones/RendimientoEquipoPage.tsx`
- Create: `frontend/src/features/puntuaciones/RendimientoEquipoPage.test.tsx`
- Modify: `frontend/src/app/App.tsx` (ruta)
- Modify: `frontend/src/shell/navConfig.tsx` (área nav "Puntuaciones")

**Interfaces:**
- Consumes: `getRendimientoEquipo`/`RendimientoEquipoDto`/`PuntuacionesApiError` (T1).
- Produces: ruta `puntuaciones/equipos`; área de nav nueva.

- [ ] **Step 1: Crear la página** — `frontend/src/features/puntuaciones/RendimientoEquipoPage.tsx`:

```tsx
// Rendimiento histórico de un equipo (HU-49/RF-44): posición y victoria por partida
// terminada. Entrada por equipoId hasta que exista la vista web de equipos.
import { useState } from "react";
import {
  getRendimientoEquipo,
  PuntuacionesApiError,
  type RendimientoEquipoDto
} from "../../api/puntuacionesApi";

const GUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

type Estado =
  | { status: "inicial" }
  | { status: "cargando" }
  | { status: "ok"; rendimiento: RendimientoEquipoDto }
  | { status: "error"; message: string };

export function RendimientoEquipoPage({ accessToken }: { accessToken: string }) {
  const [equipoId, setEquipoId] = useState("");
  const [formError, setFormError] = useState<string | null>(null);
  const [estado, setEstado] = useState<Estado>({ status: "inicial" });

  async function onConsultar(e: React.FormEvent) {
    e.preventDefault();
    const id = equipoId.trim();
    if (!GUID_RE.test(id)) {
      setFormError("Ingresa un ID de equipo válido (GUID).");
      return;
    }
    setFormError(null);
    setEstado({ status: "cargando" });
    try {
      const rendimiento = await getRendimientoEquipo(id, accessToken);
      setEstado({ status: "ok", rendimiento });
    } catch (caught) {
      setEstado({
        status: "error",
        message:
          caught instanceof PuntuacionesApiError
            ? caught.message
            : "Error inesperado al consultar el rendimiento."
      });
    }
  }

  return (
    <div className="page" data-testid="rendimiento-equipo">
      <div className="card stack">
        <h1>Rendimiento de equipo</h1>
        <form className="compact-actions" onSubmit={(e) => void onConsultar(e)}>
          <label>
            ID del equipo{" "}
            <input
              value={equipoId}
              aria-label="ID del equipo"
              placeholder="00000000-0000-0000-0000-000000000000"
              onChange={(e) => setEquipoId(e.target.value)}
            />
          </label>
          <button type="submit" disabled={estado.status === "cargando"}>
            Consultar
          </button>
        </form>
        {formError ? (
          <div className="notice error" role="alert">
            {formError}
          </div>
        ) : null}

        {estado.status === "cargando" ? <p className="muted">Consultando…</p> : null}
        {estado.status === "error" ? (
          <div className="notice error" role="alert">
            {estado.message}
          </div>
        ) : null}
        {estado.status === "ok" ? (
          estado.rendimiento.partidas.length === 0 ? (
            <p className="muted">El equipo no tiene participaciones en partidas terminadas.</p>
          ) : (
            <div className="table-wrap">
              <table aria-label="Rendimiento del equipo" data-testid="tabla-rendimiento">
                <thead>
                  <tr>
                    <th scope="col">Partida</th>
                    <th scope="col">Fecha fin</th>
                    <th scope="col">Posición</th>
                    <th scope="col">Ganó</th>
                  </tr>
                </thead>
                <tbody>
                  {estado.rendimiento.partidas.map((p) => (
                    <tr key={p.partidaId}>
                      <td>{p.partidaId.slice(0, 8)}</td>
                      <td>{new Date(p.fechaFin).toLocaleString()}</td>
                      <td>{p.posicion}</td>
                      <td>{p.gano ? "✓" : "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )
        ) : null}
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Tests** — `frontend/src/features/puntuaciones/RendimientoEquipoPage.test.tsx`:

```tsx
import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { RendimientoEquipoPage } from "./RendimientoEquipoPage";
import * as puntuacionesApi from "../../api/puntuacionesApi";

const GUID = "11111111-2222-3333-4444-555555555555";

afterEach(() => vi.restoreAllMocks());

describe("RendimientoEquipoPage", () => {
  it("GUID inválido muestra error de formato sin llamar la api", async () => {
    const spy = vi.spyOn(puntuacionesApi, "getRendimientoEquipo");
    render(<RendimientoEquipoPage accessToken="tok" />);
    await userEvent.type(screen.getByLabelText("ID del equipo"), "no-es-guid");
    await userEvent.click(screen.getByText("Consultar"));
    expect(await screen.findByText(/ID de equipo válido/)).toBeInTheDocument();
    expect(spy).not.toHaveBeenCalled();
  });

  it("consulta y muestra la tabla de partidas", async () => {
    vi.spyOn(puntuacionesApi, "getRendimientoEquipo").mockResolvedValue({
      equipoId: GUID,
      partidas: [
        { partidaId: "aabbccdd-0000-0000-0000-000000000000", fechaFin: "2026-07-10T12:00:00Z", posicion: 1, gano: true }
      ]
    });
    render(<RendimientoEquipoPage accessToken="tok" />);
    await userEvent.type(screen.getByLabelText("ID del equipo"), GUID);
    await userEvent.click(screen.getByText("Consultar"));
    expect(await screen.findByTestId("tabla-rendimiento")).toBeInTheDocument();
    expect(screen.getByText("aabbccdd")).toBeInTheDocument();
    expect(screen.getByText("✓")).toBeInTheDocument();
  });

  it("equipo sin participaciones muestra el vacío", async () => {
    vi.spyOn(puntuacionesApi, "getRendimientoEquipo").mockResolvedValue({
      equipoId: GUID,
      partidas: []
    });
    render(<RendimientoEquipoPage accessToken="tok" />);
    await userEvent.type(screen.getByLabelText("ID del equipo"), GUID);
    await userEvent.click(screen.getByText("Consultar"));
    expect(
      await screen.findByText("El equipo no tiene participaciones en partidas terminadas.")
    ).toBeInTheDocument();
  });
});
```

- [ ] **Step 3: Verificar** — Run: `cd frontend && npx vitest run src/features/puntuaciones/RendimientoEquipoPage.test.tsx`. Expected: PASS 3/3.

- [ ] **Step 4: Ruta + nav** —
En `frontend/src/app/App.tsx`: importar `RendimientoEquipoPage` desde `../features/puntuaciones/RendimientoEquipoPage` y añadir tras la ruta del historial:

```tsx
          {
            path: "puntuaciones/equipos",
            element: (
              <RequireRole roles={roles} need="Operador" landing={landing}>
                <RendimientoEquipoPage accessToken={token} />
              </RequireRole>
            )
          },
```

En `frontend/src/shell/navConfig.tsx`, añadir al final de `NAV_AREAS` (usa iconos ya exportados por `./icons`):

```ts
  {
    id: "puntuaciones",
    label: "Puntuaciones",
    role: "Operador",
    icon: ListChecks,
    items: [{ label: "Rendimiento de equipo", path: "/puntuaciones/equipos", icon: Users }]
  }
```

(añadir `ListChecks`/`Users` al import de `./icons` si no están ya importados en el archivo).

- [ ] **Step 5: Verificar gates** — Run: `cd frontend && npm test` (148 = 145 + 3) y `npx tsc -b` (limpio) y `npm run build` (PASS).

- [ ] **Step 6: Commit**

```bash
git add frontend/src/features/puntuaciones/RendimientoEquipoPage.tsx frontend/src/features/puntuaciones/RendimientoEquipoPage.test.tsx frontend/src/app/App.tsx frontend/src/shell/navConfig.tsx
git commit -m "feat(web): rendimiento historico de equipo HU-49 (bloque 2f)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 4: Gate E2E + traceability (controller — NO subagente)

**Files:**
- Modify: `docs/04-sdd/traceability-matrix.md` (fila 2f tras la 2e-2)
- Modify: `GUIA-LEVANTAMIENTO.md` (nota cierre Bloque 2)

**Interfaces:**
- Consumes: todo lo anterior a HEAD; stack completo (infra + identity :5000 + partidas :5010 + operaciones :5020 + puntuaciones :5030 + gateway :5080); `get-token.sh` PKCE del scratchpad (base64 -w0).
- Produces: cierre del bloque (fila traceability + nota GUIA + ledger).

- [ ] **Step 1: Suites en HEAD** — `cd frontend && npm test` (148) + `npx tsc -b` + `npm run build`.
- [ ] **Step 2: Stack + tokens** — levantar (o reutilizar) el stack completo; tokens operador y participante.
- [ ] **Step 3: E2E historial** — con una partida jugada (crear+jugar una Trivia corta si la DB no conserva las de 2e): token operador `GET {gw}/puntuaciones/partidas/{id}/historial` → 200 con `total` y `entradas[]` reales; `?tipo=PartidaIniciada` → `total` menor; `?limit=2&offset=0` → 2 entradas; **token participante → 403**; partidaId inexistente → 404.
- [ ] **Step 4: E2E rendimiento** — token operador `GET {gw}/puntuaciones/equipos/{guid-aleatorio}/rendimiento` → 200 `{partidas: []}`.
- [ ] **Step 5: Docs** — fila 2f en traceability (patrón filas previas: alcance, evidencia E2E con shapes, commits T1-T3, diferidos) + GUIA: nota "Bloque 2f: la web consulta historial de partida y rendimiento de equipo — **BLOQUE 2 COMPLETO**".
- [ ] **Step 6: Commit**

```bash
git add docs/04-sdd/traceability-matrix.md GUIA-LEVANTAMIENTO.md
git commit -m "docs(bloque2f): traceability fila 2f + nota GUIA cierre bloque 2" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```
