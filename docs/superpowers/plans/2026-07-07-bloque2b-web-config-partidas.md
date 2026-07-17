# Bloque 2b — UI web config partida multi-juego: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** El operador crea, lista y revisa partidas multi-juego (Trivia/BDT) contra el servicio Partidas vía gateway; la config vieja se retira.

**Architecture:** Cliente API nuevo (`partidasApi.ts`, patrón `identityApi`) + 3 páginas (`PartidasListPage`, `CreatePartidaPage` wizard de 3 pasos con estado local, `PartidaDetailPage`) + retiro de las 3 páginas de config viejas. Todo local hasta el envío final encadenado (header → juegos en orden) con reintento de restantes.

**Tech Stack:** React 18 + Vite + TypeScript, react-router (createBrowserRouter existente), vitest + testing-library, design system CSS propio (clases existentes).

**Spec:** `docs/superpowers/specs/2026-07-07-bloque2b-web-config-partidas-design.md`
**Contrato (autoridad, NO tocar):** `contracts/http/partidas-config.md`

## Global Constraints

- Rama: `feature/bloque-2`. Commits con trailer `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- PROHIBIDO a subagentes: `git stash/reset/checkout/restore/clean`. Stage SOLO archivos exactos, uno por uno.
- Cero cambios en `contracts/`, backend, dominio, gateway.
- Base URL: SIEMPRE `VITE_GATEWAY_BASE_URL` (nunca URLs directas de servicio).
- Enums exactos del contrato: `Individual|Equipo`, `Manual|Automatico|ManualYAutomatico`, `Trivia|BusquedaDelTesoro`.
- `orden` (juegos y etapas) SIEMPRE derivado de la posición (contiguo desde 1) — jamás editable.
- Reusar clases del design system (`docs/02-project-context/design/design-system.md`): `.page`, `.form-section`, `.form-section__title`, `.question-card`, `.q-title`, `.q-badge`, `.create-head`, `.create-actions`, `.notice error|success|info`, `.pill`, `.table-wrap`, `.actions`, `.secondary-button`, `.ghost-button`. NO inventar primitivas nuevas ni CSS nuevo salvo que un layout lo exija (y entonces en `create-forms.css`).
- Textos de UI en español.
- `trivia/operar` (`TriviaOperationsPage`) y `bdt/partidas` (`PublishedBdtGamesPage`) NO se tocan.

---

### Task 1: Cliente API `partidasApi.ts`

**Files:**
- Create: `frontend/src/api/partidasApi.ts`
- Create: `frontend/src/api/partidasApi.test.ts`

**Interfaces (Produces — tareas 2-5 dependen de estos nombres exactos):**
- Tipos: `Modalidad`, `ModoInicioPartida`, `CreatePartidaRequest`, `CreatePartidaResponse`, `PreguntaPayload`, `OpcionPayload`, `AddJuegoTriviaRequest`, `EtapaPayload`, `AddJuegoBdtRequest`, `AddJuegoResponse`, `PartidaSummary`, `PartidaDetail`, `JuegoDetail`, `PartidasApiError`
- Funciones: `createPartida(payload, accessToken, fetchImpl?)`, `addJuegoTrivia(partidaId, payload, accessToken, fetchImpl?)`, `addJuegoBdt(partidaId, payload, accessToken, fetchImpl?)`, `getPartida(partidaId, accessToken, fetchImpl?)`, `getPartidas(accessToken, fetchImpl?)`

- [ ] **Step 1: Tests que fallan.** Crear `frontend/src/api/partidasApi.test.ts`:

```typescript
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  addJuegoBdt,
  addJuegoTrivia,
  createPartida,
  getPartida,
  getPartidas,
  PartidasApiError
} from "./partidasApi";

const okJson = (body: unknown, status = 200) =>
  vi.fn().mockResolvedValue(new Response(JSON.stringify(body), { status }));

describe("partidasApi", () => {
  beforeEach(() => {
    vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test/");
  });
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("createPartida hace POST /partidas con bearer y devuelve partidaId", async () => {
    const fetchImpl = okJson({ partidaId: "p-1" }, 201);
    const result = await createPartida(
      {
        nombrePartida: "Copa",
        modalidad: "Individual",
        modoInicioPartida: "Manual",
        tiempoInicio: null,
        minimosParticipacion: 1,
        maximosParticipacion: 10
      },
      "tok",
      fetchImpl
    );
    expect(result.partidaId).toBe("p-1");
    const [url, init] = fetchImpl.mock.calls[0];
    expect(url).toBe("https://gw.example.test/partidas");
    expect(init.method).toBe("POST");
    expect((init.headers as Record<string, string>).Authorization).toBe("Bearer tok");
  });

  it("addJuegoTrivia y addJuegoBdt pegan a la subruta correcta", async () => {
    const fetchImpl = okJson({ juegoId: "j-1" }, 201);
    await addJuegoTrivia("p-1", { orden: 1, preguntas: [] }, "tok", fetchImpl);
    expect(fetchImpl.mock.calls[0][0]).toBe("https://gw.example.test/partidas/p-1/juegos/trivia");
    await addJuegoBdt("p-1", { orden: 2, areaBusqueda: "x", etapas: [] }, "tok", fetchImpl);
    expect(fetchImpl.mock.calls[1][0]).toBe("https://gw.example.test/partidas/p-1/juegos/bdt");
  });

  it("getPartidas y getPartida hacen GET autenticado", async () => {
    const fetchImpl = okJson([]);
    await getPartidas("tok", fetchImpl);
    expect(fetchImpl.mock.calls[0][0]).toBe("https://gw.example.test/partidas");
    const fetchOne = okJson({ partidaId: "p-1", juegos: [] });
    await getPartida("p-1", "tok", fetchOne);
    expect(fetchOne.mock.calls[0][0]).toBe("https://gw.example.test/partidas/p-1");
  });

  it("error del backend lanza PartidasApiError con status y message", async () => {
    const fetchImpl = okJson({ message: "orden duplicado" }, 409);
    await expect(
      addJuegoTrivia("p-1", { orden: 1, preguntas: [] }, "tok", fetchImpl)
    ).rejects.toMatchObject({ statusCode: 409, message: "orden duplicado" });
    await expect(
      addJuegoTrivia("p-1", { orden: 1, preguntas: [] }, "tok", fetchImpl)
    ).rejects.toBeInstanceOf(PartidasApiError);
  });

  it("sin VITE_GATEWAY_BASE_URL lanza error claro", async () => {
    vi.unstubAllEnvs();
    vi.stubEnv("VITE_GATEWAY_BASE_URL", "");
    await expect(getPartidas("tok", okJson([]))).rejects.toThrow(
      "Missing VITE_GATEWAY_BASE_URL"
    );
  });
});
```

- [ ] **Step 2: Verificar que fallan.**

Run: `cd frontend && npx vitest run src/api/partidasApi.test.ts`
Expected: FAIL — módulo `./partidasApi` no existe.

- [ ] **Step 3: Implementación.** Crear `frontend/src/api/partidasApi.ts`:

```typescript
// Cliente del servicio Partidas (configuración) a través del gateway.
// Contrato: contracts/http/partidas-config.md — este archivo lo espeja, no lo redefine.

export type Modalidad = "Individual" | "Equipo";
export type ModoInicioPartida = "Manual" | "Automatico" | "ManualYAutomatico";

export interface CreatePartidaRequest {
  nombrePartida: string;
  modalidad: Modalidad;
  modoInicioPartida: ModoInicioPartida;
  tiempoInicio: string | null;
  minimosParticipacion: number;
  maximosParticipacion: number;
}

export interface CreatePartidaResponse {
  partidaId: string;
}

export interface OpcionPayload {
  texto: string;
  esCorrecta: boolean;
}

export interface PreguntaPayload {
  texto: string;
  opciones: OpcionPayload[];
  puntaje: number;
  tiempoLimiteSegundos: number;
}

export interface AddJuegoTriviaRequest {
  orden: number;
  preguntas: PreguntaPayload[];
}

export interface EtapaPayload {
  orden: number;
  codigoQREsperado: string;
  puntaje: number;
  tiempoLimiteSegundos: number;
}

export interface AddJuegoBdtRequest {
  orden: number;
  areaBusqueda: string;
  etapas: EtapaPayload[];
}

export interface AddJuegoResponse {
  juegoId: string;
}

export interface PartidaSummary {
  partidaId: string;
  nombrePartida: string;
  modalidad: Modalidad;
  modoInicioPartida: ModoInicioPartida;
  tiempoInicio: string | null;
  minimosParticipacion: number;
  maximosParticipacion: number;
  estado: string | null;
  cantidadJuegos: number;
}

export interface OpcionDetail {
  opcionId: string;
  texto: string;
  esCorrecta: boolean;
}

export interface PreguntaDetail {
  preguntaId: string;
  texto: string;
  puntajeAsignado: number;
  tiempoLimiteSegundos: number;
  opciones: OpcionDetail[];
}

export interface EtapaDetail {
  etapaBDTId: string;
  orden: number;
  codigoQREsperado: string;
  puntajeAsignado: number;
  tiempoLimiteSegundos: number;
}

export interface JuegoDetail {
  juegoId: string;
  orden: number;
  tipoJuego: "Trivia" | "BusquedaDelTesoro";
  estado: string;
  trivia: { preguntas: PreguntaDetail[] } | null;
  bdt: { areaBusqueda: string; etapas: EtapaDetail[] } | null;
}

export interface PartidaDetail {
  partidaId: string;
  nombrePartida: string;
  modalidad: Modalidad;
  modoInicioPartida: ModoInicioPartida;
  tiempoInicio: string | null;
  minimosParticipacion: number;
  maximosParticipacion: number;
  estado: string | null;
  juegos: JuegoDetail[];
}

export class PartidasApiError extends Error {
  constructor(
    message: string,
    public readonly statusCode: number
  ) {
    super(message);
    this.name = "PartidasApiError";
  }
}

const baseUrl = import.meta.env.VITE_GATEWAY_BASE_URL as string | undefined;

function resolveBaseUrl(): string {
  const value = (import.meta.env.VITE_GATEWAY_BASE_URL as string | undefined) ?? baseUrl;
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

async function request<T>(
  path: string,
  init: RequestInit,
  fetchImpl: typeof fetch
): Promise<T> {
  const response = await fetchImpl(`${resolveBaseUrl()}${path}`, init);
  const body = (await response.json().catch(() => ({}))) as T & { message?: string };
  if (!response.ok) {
    const message = body.message ?? `Partidas API error. Status=${response.status}`;
    throw new PartidasApiError(message, response.status);
  }
  return body;
}

export async function createPartida(
  payload: CreatePartidaRequest,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<CreatePartidaResponse> {
  return request<CreatePartidaResponse>(
    "/partidas",
    { method: "POST", headers: buildAuthHeaders(accessToken), body: JSON.stringify(payload) },
    fetchImpl
  );
}

export async function addJuegoTrivia(
  partidaId: string,
  payload: AddJuegoTriviaRequest,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<AddJuegoResponse> {
  return request<AddJuegoResponse>(
    `/partidas/${partidaId}/juegos/trivia`,
    { method: "POST", headers: buildAuthHeaders(accessToken), body: JSON.stringify(payload) },
    fetchImpl
  );
}

export async function addJuegoBdt(
  partidaId: string,
  payload: AddJuegoBdtRequest,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<AddJuegoResponse> {
  return request<AddJuegoResponse>(
    `/partidas/${partidaId}/juegos/bdt`,
    { method: "POST", headers: buildAuthHeaders(accessToken), body: JSON.stringify(payload) },
    fetchImpl
  );
}

export async function getPartida(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<PartidaDetail> {
  return request<PartidaDetail>(
    `/partidas/${partidaId}`,
    { method: "GET", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}

export async function getPartidas(
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<PartidaSummary[]> {
  return request<PartidaSummary[]>(
    "/partidas",
    { method: "GET", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}
```

- [ ] **Step 4: Verificar que pasan.**

Run: `cd frontend && npx vitest run src/api/partidasApi.test.ts`
Expected: PASS 5/5.

- [ ] **Step 5: Commit.**

```bash
git add frontend/src/api/partidasApi.ts frontend/src/api/partidasApi.test.ts
git commit -m "feat(web): cliente partidasApi via gateway (bloque 2b)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 2: Draft del wizard — estado, helpers puros y validadores

**Files:**
- Create: `frontend/src/features/partidas/createPartidaDraft.ts`
- Create: `frontend/src/features/partidas/createPartidaDraft.test.ts`

**Interfaces:**
- Consumes (Task 1): tipos `CreatePartidaRequest`, `AddJuegoTriviaRequest`, `AddJuegoBdtRequest`, `Modalidad`, `ModoInicioPartida` de `../../api/partidasApi`.
- Produces (Tasks 3-4 dependen de estos nombres exactos):
  - Tipos: `HeaderDraft`, `OpcionDraft`, `PreguntaDraft`, `EtapaDraft`, `JuegoDraft` (union con `tipo: "Trivia" | "BusquedaDelTesoro"` y `localId: string`), `CreatePartidaDraft` (`{ step: 1|2|3; header: HeaderDraft; juegos: JuegoDraft[] }`)
  - `initialDraft(): CreatePartidaDraft`
  - Fábricas: `newJuegoTrivia()`, `newJuegoBdt()`, `newPregunta()`, `newEtapa()`
  - Validadores (devuelven `string[]` de errores, vacío = válido): `validateHeader(header)`, `validateJuego(juego)`, `validateDraft(draft)`
  - Builders: `buildCreatePartidaRequest(header): CreatePartidaRequest`, `buildJuegoRequest(juego, orden): AddJuegoTriviaRequest | AddJuegoBdtRequest` (discriminar por `juego.tipo`)

Notas de diseño que el implementer debe respetar:
- Campos numéricos del draft son **string** (form-friendly); los builders convierten con `Number(...)`.
- `HeaderDraft`: `{ nombrePartida: string; modalidad: Modalidad; modoInicioPartida: ModoInicioPartida; tiempoInicio: string; minimosParticipacion: string; maximosParticipacion: string }` — `tiempoInicio` es el value crudo del `<input type="datetime-local">`.
- `PreguntaDraft`: `{ texto: string; opciones: OpcionDraft[]; puntaje: string; tiempoLimiteSegundos: string }` con `OpcionDraft = { texto: string; esCorrecta: boolean }`. `newPregunta()` arranca con 2 opciones vacías, la primera `esCorrecta: true`.
- `EtapaDraft`: `{ codigoQREsperado: string; puntaje: string; tiempoLimiteSegundos: string }`.
- `localId`: `crypto.randomUUID()` — solo para keys de React, jamás viaja al backend.
- Reglas de `validateHeader` (espejo del contrato, mensajes en español): nombre no vacío; `min ≥ 1` entero; `max ≥ min`; si `modoInicioPartida !== "Manual"` → `tiempoInicio` no vacío; si es `"Manual"` → se ignora el campo.
- Reglas de `validateJuego`: Trivia → ≥1 pregunta; cada pregunta: texto no vacío, ≥2 opciones todas con texto, exactamente una `esCorrecta`, `puntaje > 0`, `tiempo > 0`. BDT → `areaBusqueda` no vacía, ≥1 etapa; cada etapa: `codigoQREsperado` no vacío, `puntaje > 0`, `tiempo > 0`.
- `validateDraft`: `validateHeader` + ≥1 juego + cada `validateJuego`.
- `buildCreatePartidaRequest`: `tiempoInicio: null` si modo `Manual`, si no `new Date(header.tiempoInicio).toISOString()`.
- `buildJuegoRequest`: `orden` viene por parámetro (posición+1); en BDT las etapas llevan `orden` = índice+1.

- [ ] **Step 1: Tests que fallan.** Crear `createPartidaDraft.test.ts` con estos casos (código completo, estilo vitest puro sin DOM):
  1. `initialDraft` arranca en step 1, header vacío con `modalidad: "Individual"` y `modoInicioPartida: "Manual"`, cero juegos.
  2. `validateHeader` rechaza: nombre vacío; `min=0`; `max < min`; modo `Automatico` sin `tiempoInicio`. Acepta el caso feliz Manual sin tiempo.
  3. `validateJuego` Trivia rechaza: sin preguntas; pregunta con 1 opción; con dos correctas; con ninguna correcta; puntaje 0. Acepta pregunta bien formada.
  4. `validateJuego` BDT rechaza: área vacía; sin etapas; etapa sin QR; tiempo 0. Acepta caso feliz.
  5. `buildCreatePartidaRequest` con modo Manual → `tiempoInicio: null`; con Automatico → ISO string.
  6. `buildJuegoRequest` Trivia produce `{ orden, preguntas }` con números convertidos; BDT produce etapas con `orden` contiguo desde 1.

- [ ] **Step 2: Verificar que fallan.** Run: `cd frontend && npx vitest run src/features/partidas/createPartidaDraft.test.ts` → FAIL (módulo no existe).

- [ ] **Step 3: Implementar `createPartidaDraft.ts`** cumpliendo exactamente las Interfaces/notas de arriba. Sin dependencias nuevas. Funciones puras, sin React.

- [ ] **Step 4: Verificar que pasan.** Run: mismo comando → PASS.

- [ ] **Step 5: Commit.**

```bash
git add frontend/src/features/partidas/createPartidaDraft.ts frontend/src/features/partidas/createPartidaDraft.test.ts
git commit -m "feat(web): draft + validadores del wizard de partida (bloque 2b)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 3: Envío encadenado — `enviarPartida`

**Files:**
- Create: `frontend/src/features/partidas/enviarPartida.ts`
- Create: `frontend/src/features/partidas/enviarPartida.test.ts`

**Interfaces:**
- Consumes: Task 1 (`createPartida`, `addJuegoTrivia`, `addJuegoBdt`, `PartidasApiError`), Task 2 (`CreatePartidaDraft`, `buildCreatePartidaRequest`, `buildJuegoRequest`).
- Produces (Task 4 depende de esto):

```typescript
export type EstadoEnvio = "pendiente" | "enviando" | "ok" | "error";

export interface EnvioJuego {
  estado: EstadoEnvio;
  mensaje?: string;
}

export interface ResultadoEnvio {
  partidaId: string | null;   // null si falló el header
  estados: EnvioJuego[];      // uno por juego, mismo orden que draft.juegos
  completo: boolean;          // true si header + todos los juegos en "ok"
  errorHeader?: string;       // mensaje si falló el POST /partidas
}

export async function enviarPartida(
  draft: CreatePartidaDraft,
  accessToken: string,
  previo: { partidaId: string | null; estados: EnvioJuego[] } | null,
  onProgress: (estados: EnvioJuego[], partidaId: string | null) => void,
  deps?: {
    createPartida?: typeof createPartida;
    addJuegoTrivia?: typeof addJuegoTrivia;
    addJuegoBdt?: typeof addJuegoBdt;
  }
): Promise<ResultadoEnvio>
```

Algoritmo (obligatorio, es el corazón del slice):
1. Si `previo?.partidaId` existe → reusar ese `partidaId` (NO re-POSTear el header). Si no → `createPartida(buildCreatePartidaRequest(draft.header))`; si falla → retornar `{ partidaId: null, estados: todos "pendiente", completo: false, errorHeader: mensaje }`.
2. Estados iniciales: los de `previo` si vienen (normalizando `"error"`/`"enviando"` → `"pendiente"`), si no todos `"pendiente"`.
3. Para cada juego `i` en orden: si `estados[i] === "ok"` → saltar (nunca re-enviar un 201 — el 409 por `orden` duplicado es exactamente lo que esto evita). Si no → marcar `"enviando"`, `onProgress`, llamar `addJuegoTrivia`/`addJuegoBdt` con `buildJuegoRequest(juego, i + 1)`; 201 → `"ok"`; excepción → `"error"` con `mensaje` (de `PartidasApiError.message` o genérico de red) y **detener la cadena** (los siguientes quedan `"pendiente"`).
4. `onProgress` tras cada transición de estado. Retornar `completo = estados.every(e => e.estado === "ok")`.

- [ ] **Step 1: Tests que fallan.** Casos (mock de deps por parámetro `deps`, sin fetch real):
  1. Flujo feliz: header + trivia + bdt → 3 llamadas, `completo: true`, estados `["ok","ok"]`, órdenes 1 y 2.
  2. Falla el header → `errorHeader` con el mensaje, cero llamadas a juegos.
  3. Falla el juego 2 de 3 → estados `["ok","error","pendiente"]`, `completo: false`, cadena detenida (el juego 3 no se llamó).
  4. Reintento con `previo` (`partidaId: "p-1"`, estados `["ok","error","pendiente"]`) → NO llama `createPartida`, NO re-envía juego 1, envía juegos 2 y 3 → `completo: true`.
  5. `onProgress` se llamó con `"enviando"` antes de cada POST de juego.

- [ ] **Step 2: Verificar que fallan.** Run: `cd frontend && npx vitest run src/features/partidas/enviarPartida.test.ts` → FAIL.

- [ ] **Step 3: Implementar `enviarPartida.ts`** según el algoritmo. `deps` con defaults a las funciones reales.

- [ ] **Step 4: Verificar que pasan.** → PASS.

- [ ] **Step 5: Commit.**

```bash
git add frontend/src/features/partidas/enviarPartida.ts frontend/src/features/partidas/enviarPartida.test.ts
git commit -m "feat(web): envio encadenado con reintento de restantes (bloque 2b)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 4: `CreatePartidaPage` — wizard de 3 pasos

**Files:**
- Create: `frontend/src/features/partidas/CreatePartidaPage.tsx`
- Create: `frontend/src/features/partidas/CreatePartidaPage.test.tsx`

**Interfaces:**
- Consumes: Task 2 completo; Task 3 (`enviarPartida`, `EnvioJuego`, `ResultadoEnvio`); `useNavigate` de react-router-dom.
- Produces: `export function CreatePartidaPage({ accessToken }: { accessToken: string })` — Task 6 la registra en la ruta `partidas/crear`.

Estructura obligatoria (los detalles de JSX los decide el implementer siguiendo el design system):
- `useReducer` fino: estado = `CreatePartidaDraft` + `{ envio: { partidaId: string | null; estados: EnvioJuego[]; errorHeader?: string } | null; enviando: boolean }`. Acciones gruesas: `{ type: "patchHeader", patch }`, `{ type: "setJuegos", juegos }` (los helpers puros de Task 2 calculan el array siguiente), `{ type: "setStep", step }`, `{ type: "envioProgreso", estados, partidaId }`, `{ type: "envioResultado", resultado }`, `{ type: "enviando" }`.
- **Paso 1** (`.form-section`): inputs de header. `tiempoInicio` (`<input type="datetime-local">`) solo visible si `modoInicioPartida !== "Manual"`. Botón "Siguiente" ejecuta `validateHeader`; errores en `.notice error`; no avanza con errores.
- **Paso 2**: lista de juegos como `.question-card` (título "Juego N — Trivia/Búsqueda del Tesoro", `.q-badge` con el tipo). Botones "Agregar Trivia" / "Agregar BDT" (fábricas de Task 2). Por juego: subir/bajar (`.ghost-button`, deshabilitado en extremos), eliminar. Editor Trivia: preguntas con texto, opciones dinámicas (agregar/quitar; radio `name` único por pregunta para `esCorrecta`), puntaje y tiempo. Editor BDT: `areaBusqueda` + etapas (QR esperado como TEXTO — sin upload de imagen; puntaje; tiempo). "Siguiente" valida ≥1 juego + `validateJuego` de cada uno.
- **Paso 3**: resumen de solo lectura (header + juegos con conteos y detalle) + botón "Crear partida" (`.create-actions`). Durante envío: estado por juego visible (pendiente/enviando/ok/error — usar `.pill` con modificadores existentes) + `errorHeader` si aplica. En fallo parcial: botón "Reintentar restantes" que llama `enviarPartida` con `previo`. En `completo: true` → `navigate(\`/partidas/\${partidaId}\`)`.
- `data-testid` obligatorios (los tests y 2c dependen de ellos): `paso-1`, `paso-2`, `paso-3`, `btn-siguiente`, `btn-atras`, `btn-agregar-trivia`, `btn-agregar-bdt`, `btn-crear-partida`, `btn-reintentar`, `envio-juego-{i}` (contenedor del estado del juego i, 0-based).

- [ ] **Step 1: Tests que fallan.** `CreatePartidaPage.test.tsx` con testing-library (patrón de los tests existentes del repo: `render`, `screen`, `fireEvent`/`userEvent`), mocks de `enviarPartida` vía `vi.mock("./enviarPartida")`:
  1. Paso 1 no avanza con nombre vacío (aparece `.notice`), avanza con datos válidos Manual.
  2. Con modo `Automatico`, el campo `tiempoInicio` aparece y es obligatorio para avanzar.
  3. Paso 2: agregar una Trivia y un BDT crea dos cards en orden ("Juego 1", "Juego 2"); subir el segundo lo vuelve "Juego 1".
  4. Paso 2 no avanza con pregunta inválida (1 opción); avanza con juegos válidos.
  5. Paso 3 muestra el resumen; click en `btn-crear-partida` llama `enviarPartida` con el draft y navega al detalle en éxito (`useNavigate` mockeado vía `vi.mock("react-router-dom", ...)` parcial o wrapper `MemoryRouter` + assert de location — seguir el patrón de tests existentes del repo).
  6. Fallo parcial (mock devuelve `completo: false`, estados `["ok","error"]`) → aparece `btn-reintentar`; click lo llama con `previo` correcto.

- [ ] **Step 2: Verificar que fallan.** Run: `cd frontend && npx vitest run src/features/partidas/CreatePartidaPage.test.tsx` → FAIL.

- [ ] **Step 3: Implementar la página.** Respetar estructura, testids y clases. Sin CSS nuevo salvo necesidad real (y entonces en `frontend/src/styles/create-forms.css` — verificar la ruta real de los css al implementar).

- [ ] **Step 4: Verificar que pasan + typecheck.** Run: `cd frontend && npx vitest run src/features/partidas/ && npx tsc --noEmit` → PASS.

- [ ] **Step 5: Commit.**

```bash
git add frontend/src/features/partidas/CreatePartidaPage.tsx frontend/src/features/partidas/CreatePartidaPage.test.tsx
git commit -m "feat(web): wizard de creacion de partida multi-juego (bloque 2b)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 5: `PartidasListPage` + `PartidaDetailPage`

**Files:**
- Create: `frontend/src/features/partidas/PartidasListPage.tsx`
- Create: `frontend/src/features/partidas/PartidasListPage.test.tsx`
- Create: `frontend/src/features/partidas/PartidaDetailPage.tsx`
- Create: `frontend/src/features/partidas/PartidaDetailPage.test.tsx`

**Interfaces:**
- Consumes: Task 1 (`getPartidas`, `getPartida`, tipos `PartidaSummary`, `PartidaDetail`); react-router (`Link`, `useParams`, `useNavigate`).
- Produces: `export function PartidasListPage({ accessToken })` y `export function PartidaDetailPage({ accessToken })` — Task 6 las registra.

Estructura obligatoria:
- **Lista**: `.page` con `.create-head` (título "Partidas" + botón "Nueva partida" → `/partidas/crear`). Tabla en `.table-wrap`: Nombre, Modalidad, Modo de inicio, Juegos (`cantidadJuegos`), Estado (`estado ?? "Sin publicar"` en `.pill`). Fila clickeable → `/partidas/{partidaId}` (patrón `.row-link` si existe en las tablas actuales). Estados de carga (`.ops-skel` o texto), error (`.notice error` + botón reintentar) y vacío (`.empty-panel`).
- **Detalle**: `useParams` para `partidaId`; `getPartida`; header con nombre + pills (modalidad, modo, estado, min/max); juegos ordenados como `.question-card`: Trivia → preguntas con opciones marcando la correcta (badge "Correcta"); BDT → área de búsqueda + tabla de etapas (orden, QR esperado, puntaje, tiempo). 404 → `.notice error` "Partida no encontrada" + link a la lista.
- `data-testid`: `lista-partidas`, `fila-partida-{partidaId}`, `btn-nueva-partida`, `detalle-partida`, `juego-{orden}`.

- [ ] **Step 1: Tests que fallan.** Con `vi.mock("../../api/partidasApi")`:
  - Lista: render con 2 summaries (muestra nombres y "Sin publicar" para `estado: null`); estado vacío; error de API muestra `.notice`.
  - Detalle: render con la fixture del contrato (partida con 1 trivia de 1 pregunta/2 opciones + 1 bdt de 1 etapa) → muestra ambos juegos en orden, la opción correcta marcada, la etapa con su QR; 404 → mensaje.
  Envolver en `MemoryRouter` con la ruta parametrizada (patrón de tests existentes).

- [ ] **Step 2: Verificar que fallan.** Run: `cd frontend && npx vitest run src/features/partidas/PartidasListPage.test.tsx src/features/partidas/PartidaDetailPage.test.tsx` → FAIL.

- [ ] **Step 3: Implementar ambas páginas.**

- [ ] **Step 4: Verificar que pasan.** → PASS.

- [ ] **Step 5: Commit.**

```bash
git add frontend/src/features/partidas/PartidasListPage.tsx frontend/src/features/partidas/PartidasListPage.test.tsx frontend/src/features/partidas/PartidaDetailPage.tsx frontend/src/features/partidas/PartidaDetailPage.test.tsx
git commit -m "feat(web): lista y detalle de partidas (bloque 2b)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 6: Rutas + nav + retiro de la config vieja

**Files:**
- Modify: `frontend/src/app/App.tsx` (imports líneas 13-20, rutas líneas 118-149)
- Modify: `frontend/src/shell/navConfig.tsx`
- Delete: `frontend/src/features/trivia/CreateTriviaGamePage.tsx` + `.test.tsx`
- Delete: `frontend/src/features/trivia/CreateTriviaFormPage.tsx` + `.test.tsx`
- Delete: `frontend/src/features/bdt/CreateBdtGamePage.tsx` + `.test.tsx`
- Modify: `frontend/src/api/triviaApi.ts` (retirar `createTriviaGame`, `createTriviaForm`, `getTriviaForms` + tipos que queden huérfanos) y su test si cubre esas funciones
- Modify: `frontend/src/api/bdtApi.ts` (retirar `createBdtGame`, `decodeBdtExpectedQrImage` + tipos huérfanos) y su test si cubre esas funciones

**Interfaces:**
- Consumes: Tasks 4-5 (`CreatePartidaPage`, `PartidasListPage`, `PartidaDetailPage`).

- [ ] **Step 1: Rutas nuevas en `App.tsx`.** Importar las 3 páginas de `../features/partidas/...`; eliminar los imports de `CreateBdtGamePage`, `CreateTriviaFormPage`, `CreateTriviaGamePage`. Reemplazar los bloques de rutas `trivia/formularios/nuevo`, `trivia/crear` y `bdt/crear` por:

```tsx
          {
            path: "partidas",
            element: (
              <RequireRole roles={roles} need="Operador" landing={landing}>
                <PartidasListPage accessToken={token} />
              </RequireRole>
            )
          },
          {
            path: "partidas/crear",
            element: (
              <RequireRole roles={roles} need="Operador" landing={landing}>
                <CreatePartidaPage accessToken={token} />
              </RequireRole>
            )
          },
          {
            path: "partidas/:partidaId",
            element: (
              <RequireRole roles={roles} need="Operador" landing={landing}>
                <PartidaDetailPage accessToken={token} />
              </RequireRole>
            )
          },
```

(`trivia/operar`, `bdt/partidas` y todo lo de identidad quedan tal cual.)

- [ ] **Step 2: Nav (`navConfig.tsx`).** Nueva área al inicio de las de Operador:

```tsx
  {
    id: "partidas",
    label: "Partidas",
    role: "Operador",
    icon: Flag,
    items: [
      { label: "Partidas", path: "/partidas", icon: ListChecks },
      { label: "Nueva partida", path: "/partidas/crear", icon: Plus }
    ]
  },
```

Retirar los items "Crear formulario", "Crear Trivia" (queda solo "Operar Trivia" en el área trivia) y "Crear BDT" (queda "Partidas BDT"). Quitar del import de iconos los que queden sin uso (`ClipboardList` si nadie más lo usa). `landingPath`: Operador pasa de `/trivia/operar` a `/partidas`. `titleForPath`: agregar antes del return final: `if (pathname.startsWith("/partidas/") ) return "Detalle de partida";` (la lista y crear ya matchean por items).

- [ ] **Step 3: Borrar páginas viejas y podar APIs.** `git rm` de los 6 archivos listados. En `triviaApi.ts`/`bdtApi.ts` eliminar SOLO las funciones listadas y los tipos exclusivos de ellas; en sus tests, eliminar los casos de esas funciones. Verificación obligatoria:

```bash
grep -rn "createTriviaGame\|createTriviaForm\|getTriviaForms\|createBdtGame\|decodeBdtExpectedQrImage\|CreateTriviaGamePage\|CreateTriviaFormPage\|CreateBdtGamePage" frontend/src ; echo "exit=$?"
```

Expected: sin matches (`exit=1`).

- [ ] **Step 4: Suite completa + build.**

Run: `cd frontend && npm test && npm run build`
Expected: PASS todo y build limpio (el build pesca imports rotos).

- [ ] **Step 5: Commit.**

```bash
git add frontend/src/app/App.tsx frontend/src/shell/navConfig.tsx frontend/src/api/triviaApi.ts frontend/src/api/bdtApi.ts
git add -u frontend/src/features/trivia frontend/src/features/bdt frontend/src/api
git commit -m "feat(web): rutas partidas + nav; retiro de config vieja trivia/bdt (bloque 2b)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 7: Verificación E2E viva (controller — NO subagente)

**Files:** ninguno.

- [ ] **Step 1: Stack.** Infra compose + `services/partidas/run-local.sh` + `services/identity-service/run-local.sh` + `gateway/run-local.sh`.
- [ ] **Step 2: E2E de contrato vía gateway** con token de operador (flujo PKCE por curl como en 2a): `POST /partidas` (Individual, Manual) → `POST juegos/trivia` (2 preguntas) → `POST juegos/bdt` (2 etapas) → `GET /partidas/{id}` muestra los 2 juegos ordenados → `GET /partidas` la lista con `cantidadJuegos: 2`.
- [ ] **Step 3: Smoke UI.** `frontend/run-local.sh`; verificación del flujo en navegador (login operador → nueva partida → wizard completo → detalle). Si no hay navegador disponible en la sesión, dejar constancia y pedir el pase visual al usuario.
- [ ] **Step 4: Caso 409 real:** repetir un `POST juegos/trivia` con el mismo `orden` → 409 (valida el mensaje que la UI muestra en fallo parcial).

---

### Task 8: Docs + traceability

**Files:**
- Modify: `docs/04-sdd/traceability-matrix.md` (fila Bloque 2b)
- Modify: `GUIA-LEVANTAMIENTO.md` (nota: config de juegos ahora en web vía `partidas`; levantar `services/partidas` para usarla)

- [ ] **Step 1:** Fila Bloque 2b: HU-45 + HU-13 + HU-28 (lado config/web) — evidencia: spec, commits del slice, suites frontend verdes, E2E de contrato vía gateway PASS. Formato de la fila 2a como referencia.
- [ ] **Step 2:** En GUIA, en la sección del levantamiento, tras la nota del gateway de 2a, añadir una línea: la creación/configuración de partidas vive en la web (`/partidas`) contra `services/partidas` — incluir `./services/partidas/run-local.sh` en el levantamiento del flujo de configuración.
- [ ] **Step 3: Commit.**

```bash
git add docs/04-sdd/traceability-matrix.md GUIA-LEVANTAMIENTO.md
git commit -m "docs(bloque2b): traceability HU-45/13/28 config + guia partidas

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Modelos y reviewers (workflow subagent-driven)

| Task | Implementer | Review |
|---|---|---|
| T1 | haiku (código verbatim en el plan) | sonnet |
| T2 | sonnet (implementa contra interfaces + tests dictados) | sonnet |
| T3 | sonnet (algoritmo dictado, implementación con juicio) | sonnet |
| T4 | sonnet (UI compleja) | sonnet |
| T5 | sonnet (2 páginas) | sonnet |
| T6 | sonnet (multi-archivo + retiros) | sonnet |
| T7 | controller | — |
| T8 | haiku | sonnet |

Review final whole-branch: opus.
