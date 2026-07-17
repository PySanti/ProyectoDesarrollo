# Bloque 2e-1 — Mobile gameplay Trivia + estructura live Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** El participante mobile juega Trivia en vivo: pantalla live con pregunta + responder una vez + countdown + ranking GET-en-señal + consolidado al finalizar; navegación lobby→live; fix del minor 2d (miembro no-líder en lobby Equipo).

**Architecture:** Solo mobile — sin cambios backend/contrato. Feature `features/partidas/` crece: `gameplayApi.js` (result objects), `partidaLiveFlow.js`, `liveShared.tsx` (Countdown + RankingTable — compartidos con el BdtPlayPanel de 2e-2; lección F-2 de 2c-4: 2 consumidores ciertos → extraer de una), `PartidaLiveScreen.tsx` + `TriviaPlayPanel.tsx`, y ediciones quirúrgicas a lobby/panel para navegar al live.

**Tech Stack:** React Native + Expo SDK 54, `@microsoft/signalr@^8` (ya instalado), `node --test` (flows), `tsc --noEmit`.

## Global Constraints

- **Sin cambios backend ni de contrato.** Los endpoints/eventos ya existen (2c/2d/SP-3).
- API modules patrón exacto del repo: `(apiBaseUrl, token, ..., fetchImpl = fetch)`, result objects sin throws, mensajes español sin acentos; reusar `mapCommonError`/`networkError` de `partidasPublicadasApi.js`.
- Tests `node --test` con imports **ESM** `.js` (package.json `"type": "module"`); screens `.tsx` con casts de unión discriminada para puentear flows `.js` (idioma establecido en `PartidaLobbyScreen.tsx:26-33`).
- Shared-ui REAL: `Button label=` (no children) + `variant`/`disabled`/`onPress`; `Notice variant=`; `AppText variant=`; `Card`, `ScreenHeader`; theme `colors`/`spacing`; iconos Feather.
- Gate mobile: `npm test` + `npm run typecheck` verdes. Baseline pre-T1: **61/61 + tsc limpio** (HEAD 7979bb1).
- Cada commit termina con trailer: `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- Subagents PROHIBIDO `git stash/reset/checkout/restore/clean`. Solo `git add`/`git rm` por ruta exacta.

**Shapes de referencia (del contrato/código vivo, para todos los tasks):**
- `MiSesionDto` (camelCase): `{partidaId, sesionPartidaId, estadoPartida: "Lobby"|"Iniciada", modalidad, inscripcion, juegoActivo: {juegoId, orden, tipoJuego: "Trivia"|"BusquedaDelTesoro", estadoJuego} | null, preguntaActual, etapaActual, yaRespondioPreguntaActual: boolean|null, convocatoria}`.
- `PreguntaActualDto`: `{partidaId, juegoId, preguntaId, orden, texto, opciones: [{opcionId, texto}], fechaActivacion, tiempoLimiteSegundos}` (SIN campo de correcta — participant-safe).
- `RespuestaTriviaResponse`: `{partidaId, preguntaId, esCorrecta, cerroPregunta, puntaje?}`.
- Ranking juego: `{juegoId, tipoJuego, generadoEn, entradas: [{posicion, competidorId, tipoCompetidor, puntos, tiempoAcumuladoMs, unidadesGanadas}]}`.
- Consolidado: `{partidaId, generadoEn, entradas: [{posicion, competidorId, tipoCompetidor, juegosGanados, puntosTotales, tiempoTotalMs}]}`.
- `GET /identity/teams/mine` → `{equipoId, nombreEquipo, estado, liderUserId, integrantes: [...]}` · 404 sin equipo.

---

### Task 1: `gameplayApi.js`

**Files:**
- Create: `mobile/src/features/partidas/gameplayApi.js`
- Test: `mobile/tests/gameplayApi.test.js`

**Interfaces:**
- Consumes: `mapCommonError`, `networkError` de `./partidasPublicadasApi.js`.
- Produces (T2/T3 consumen estas firmas exactas):
  - `getPreguntaActual(apiBaseUrl, token, partidaId, fetchImpl?)` → `{ok:true, pregunta}` | `{ok:false, type:"sin_pregunta"}` (409) | error mapeado
  - `responderPregunta(apiBaseUrl, token, partidaId, opcionId, fetchImpl?)` → `{ok:true, data}` | error mapeado (409→conflict con body.message)
  - `getRankingJuego(apiBaseUrl, token, partidaId, juegoId, fetchImpl?)` → `{ok:true, ranking}` | error
  - `getRankingConsolidado(apiBaseUrl, token, partidaId, fetchImpl?)` → `{ok:true, ranking}` | error

- [ ] **Step 1: Tests (fallan)**

`mobile/tests/gameplayApi.test.js`:

```js
import test from "node:test";
import assert from "node:assert/strict";
import {
  getPreguntaActual,
  responderPregunta,
  getRankingJuego,
  getRankingConsolidado,
} from "../src/features/partidas/gameplayApi.js";

const jsonResponse = (status, body) => ({
  ok: status >= 200 && status < 300,
  status,
  json: async () => body,
});

test("getPreguntaActual 200 devuelve pregunta y 409 devuelve sin_pregunta", async () => {
  const okImpl = async (url, init) => {
    assert.equal(url, "http://gw/operaciones-sesion/partidas/p1/pregunta-actual");
    assert.equal(init.headers.Authorization, "Bearer tok");
    return jsonResponse(200, { preguntaId: "q1", texto: "2+2?", opciones: [] });
  };
  const r1 = await getPreguntaActual("http://gw", "tok", "p1", okImpl);
  assert.equal(r1.ok, true);
  assert.equal(r1.pregunta.preguntaId, "q1");

  const sinImpl = async () => jsonResponse(409, { message: "sin pregunta activa" });
  const r2 = await getPreguntaActual("http://gw", "tok", "p1", sinImpl);
  assert.equal(r2.ok, false);
  assert.equal(r2.type, "sin_pregunta");
});

test("responderPregunta POST body opcionId y 409 duplicada → conflict", async () => {
  const calls = [];
  const okImpl = async (url, init) => {
    calls.push({ url, method: init.method, body: init.body });
    return jsonResponse(200, { esCorrecta: true, cerroPregunta: true, puntaje: 10 });
  };
  const r1 = await responderPregunta("http://gw", "tok", "p1", "op1", okImpl);
  assert.equal(r1.ok, true);
  assert.equal(r1.data.puntaje, 10);
  assert.deepEqual(calls, [{
    url: "http://gw/operaciones-sesion/partidas/p1/pregunta-actual/respuesta",
    method: "POST",
    body: JSON.stringify({ opcionId: "op1" }),
  }]);

  const dupImpl = async () => jsonResponse(409, { message: "Ya respondiste esta pregunta." });
  const r2 = await responderPregunta("http://gw", "tok", "p1", "op1", dupImpl);
  assert.equal(r2.ok, false);
  assert.equal(r2.type, "conflict");
  assert.equal(r2.message, "Ya respondiste esta pregunta.");
});

test("getRankingJuego y getRankingConsolidado arman URLs de puntuaciones", async () => {
  const urls = [];
  const impl = async (url) => {
    urls.push(url);
    return jsonResponse(200, { entradas: [] });
  };
  const r1 = await getRankingJuego("http://gw", "tok", "p1", "j1", impl);
  assert.equal(r1.ok, true);
  const r2 = await getRankingConsolidado("http://gw", "tok", "p1", impl);
  assert.equal(r2.ok, true);
  assert.deepEqual(urls, [
    "http://gw/puntuaciones/partidas/p1/juegos/j1/ranking",
    "http://gw/puntuaciones/partidas/p1/ranking-consolidado",
  ]);
});

test("consolidado 409 (no terminada) mapea conflict", async () => {
  const impl = async () => jsonResponse(409, { message: "no terminada" });
  const r = await getRankingConsolidado("http://gw", "tok", "p1", impl);
  assert.equal(r.ok, false);
  assert.equal(r.type, "conflict");
});
```

- [ ] **Step 2: Correr, verificar FAIL**

Run: `cd mobile && node --test tests/gameplayApi.test.js`
Expected: FAIL (módulo no existe).

- [ ] **Step 3: Implementar `gameplayApi.js`**

```js
import { mapCommonError, networkError } from "./partidasPublicadasApi.js";

async function get(apiBaseUrl, token, path, fetchImpl) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}${path}`, {
      method: "GET",
      headers: { Authorization: `Bearer ${token}` },
    });
  } catch {
    return { response: null, body: null, error: networkError() };
  }
  const body = await response.json().catch(() => null);
  return { response, body, error: null };
}

export async function getPreguntaActual(apiBaseUrl, token, partidaId, fetchImpl = fetch) {
  const { response, body, error } = await get(
    apiBaseUrl, token, `/operaciones-sesion/partidas/${partidaId}/pregunta-actual`, fetchImpl,
  );
  if (error) return error;
  if (response.status === 409) {
    return { ok: false, type: "sin_pregunta", message: body?.message || "Sin pregunta activa." };
  }
  if (!response.ok) return mapCommonError(response.status, body);
  return { ok: true, pregunta: body };
}

export async function responderPregunta(apiBaseUrl, token, partidaId, opcionId, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(
      `${apiBaseUrl}/operaciones-sesion/partidas/${partidaId}/pregunta-actual/respuesta`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
        body: JSON.stringify({ opcionId }),
      },
    );
  } catch {
    return networkError();
  }
  const body = await response.json().catch(() => null);
  if (!response.ok) return mapCommonError(response.status, body);
  return { ok: true, data: body };
}

export async function getRankingJuego(apiBaseUrl, token, partidaId, juegoId, fetchImpl = fetch) {
  const { response, body, error } = await get(
    apiBaseUrl, token, `/puntuaciones/partidas/${partidaId}/juegos/${juegoId}/ranking`, fetchImpl,
  );
  if (error) return error;
  if (!response.ok) return mapCommonError(response.status, body);
  return { ok: true, ranking: body };
}

export async function getRankingConsolidado(apiBaseUrl, token, partidaId, fetchImpl = fetch) {
  const { response, body, error } = await get(
    apiBaseUrl, token, `/puntuaciones/partidas/${partidaId}/ranking-consolidado`, fetchImpl,
  );
  if (error) return error;
  if (!response.ok) return mapCommonError(response.status, body);
  return { ok: true, ranking: body };
}
```

- [ ] **Step 4: Correr tests → PASS; suite + typecheck**

Run: `cd mobile && node --test tests/gameplayApi.test.js && npm test && npm run typecheck`
Expected: 4/4 nuevos; suite 65; tsc limpio.

- [ ] **Step 5: Commit**

```bash
cd mobile && git add src/features/partidas/gameplayApi.js tests/gameplayApi.test.js
git commit -m "feat(mobile): gameplayApi pregunta/respuesta/rankings (bloque 2e1)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 2: `partidaLiveFlow.js`

**Files:**
- Create: `mobile/src/features/partidas/partidaLiveFlow.js`
- Test: `mobile/tests/partidaLiveFlow.test.js`

**Interfaces:**
- Consumes: `getMiSesion` de `./miSesionApi.js`.
- Produces: `cargarLive({apiBaseUrl, token, partidaId, fetchImpl})` →
  - `{ok:true, fase:"sin-participacion"}` (sesión null o de otra partida)
  - `{ok:true, fase:"lobby"}` (estadoPartida Lobby)
  - `{ok:true, fase:"iniciada", juegoActivo, yaRespondio}` (estadoPartida Iniciada; `juegoActivo` puede ser null entre juegos)
  - `{ok:false, type, message}` (error del api)

- [ ] **Step 1: Tests (fallan)**

`mobile/tests/partidaLiveFlow.test.js`:

```js
import test from "node:test";
import assert from "node:assert/strict";
import { cargarLive } from "../src/features/partidas/partidaLiveFlow.js";

const jsonResponse = (status, body) => ({
  ok: status >= 200 && status < 300,
  status,
  json: async () => body,
});

test("cargarLive con sesion iniciada devuelve fase iniciada + juegoActivo + yaRespondio", async () => {
  const fetchImpl = async () =>
    jsonResponse(200, {
      partidaId: "p1",
      estadoPartida: "Iniciada",
      juegoActivo: { juegoId: "j1", orden: 1, tipoJuego: "Trivia", estadoJuego: "Activo" },
      yaRespondioPreguntaActual: true,
    });
  const r = await cargarLive({ apiBaseUrl: "http://gw", token: "tok", partidaId: "p1", fetchImpl });
  assert.equal(r.ok, true);
  assert.equal(r.fase, "iniciada");
  assert.equal(r.juegoActivo.tipoJuego, "Trivia");
  assert.equal(r.yaRespondio, true);
});

test("cargarLive sin sesion o con otra partida devuelve sin-participacion", async () => {
  const sin = async () => ({ ok: true, status: 204, json: async () => ({}) });
  const r1 = await cargarLive({ apiBaseUrl: "http://gw", token: "tok", partidaId: "p1", fetchImpl: sin });
  assert.equal(r1.fase, "sin-participacion");

  const otra = async () => jsonResponse(200, { partidaId: "OTRA", estadoPartida: "Iniciada" });
  const r2 = await cargarLive({ apiBaseUrl: "http://gw", token: "tok", partidaId: "p1", fetchImpl: otra });
  assert.equal(r2.fase, "sin-participacion");
});

test("cargarLive con sesion en lobby devuelve fase lobby", async () => {
  const fetchImpl = async () => jsonResponse(200, { partidaId: "p1", estadoPartida: "Lobby" });
  const r = await cargarLive({ apiBaseUrl: "http://gw", token: "tok", partidaId: "p1", fetchImpl });
  assert.equal(r.fase, "lobby");
});
```

- [ ] **Step 2: Correr → FAIL**

Run: `cd mobile && node --test tests/partidaLiveFlow.test.js`

- [ ] **Step 3: Implementar**

```js
import { getMiSesion } from "./miSesionApi.js";

export async function cargarLive({ apiBaseUrl, token, partidaId, fetchImpl }) {
  const r = await getMiSesion(apiBaseUrl, token, fetchImpl ?? fetch);
  if (!r.ok) return r;
  if (r.sesion == null || r.sesion.partidaId !== partidaId) {
    return { ok: true, fase: "sin-participacion" };
  }
  if (r.sesion.estadoPartida === "Lobby") {
    return { ok: true, fase: "lobby" };
  }
  return {
    ok: true,
    fase: "iniciada",
    juegoActivo: r.sesion.juegoActivo ?? null,
    yaRespondio: r.sesion.yaRespondioPreguntaActual === true,
  };
}
```

- [ ] **Step 4: PASS + suite + typecheck**

Run: `cd mobile && node --test tests/partidaLiveFlow.test.js && npm test && npm run typecheck`

- [ ] **Step 5: Commit**

```bash
cd mobile && git add src/features/partidas/partidaLiveFlow.js tests/partidaLiveFlow.test.js
git commit -m "feat(mobile): partidaLiveFlow fases del live por mi-sesion (bloque 2e1)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 3: `liveShared.tsx` + `TriviaPlayPanel.tsx` + `PartidaLiveScreen.tsx` + ruta

**Files:**
- Create: `mobile/src/features/partidas/liveShared.tsx` (Countdown + RankingTable — 2e-2 los reusa para BDT)
- Create: `mobile/src/features/partidas/TriviaPlayPanel.tsx`
- Create: `mobile/src/features/partidas/PartidaLiveScreen.tsx`
- Create: `mobile/src/features/partidas/PartidaLiveScreenContainer.tsx`
- Modify: `mobile/src/navigation/types.ts` (ruta `PartidaLive: { partidaId: string; nombre: string }`)
- Modify: `mobile/src/navigation/RootNavigator.tsx` (import + screen)

**Interfaces:**
- Consumes: `cargarLive` (T2), `getPreguntaActual`/`responderPregunta`/`getRankingJuego`/`getRankingConsolidado` (T1), `crearSesionHub` (2d), `parseJwtPayload` de `../../auth/tokenClaims.js`.
- Produces: ruta `PartidaLive` navegable (T4 la usa); `Countdown({target, expiredLabel?})` y `RankingTable({entradas, resaltarId?})` exportados de `liveShared.tsx` (2e-2 los consume).

- [ ] **Step 1: `liveShared.tsx`**

```tsx
// Piezas compartidas del live del participante (Trivia 2e-1 + BDT 2e-2).
import React, { useEffect, useState } from "react";
import { StyleSheet, View } from "react-native";
import { AppText } from "../../shared/ui";
import { colors, spacing } from "../../shared/theme";

export function Countdown({ target, expiredLabel = "Tiempo agotado" }: { target: string; expiredLabel?: string }) {
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, []);
  const remaining = Math.max(0, Math.floor((new Date(target).getTime() - now) / 1000));
  const mm = String(Math.floor(remaining / 60)).padStart(2, "0");
  const ss = String(remaining % 60).padStart(2, "0");
  return <AppText>{remaining > 0 ? `⏱ ${mm}:${ss}` : expiredLabel}</AppText>;
}

export type RankingEntrada = {
  posicion: number;
  competidorId: string;
  puntos: number;
};

export function RankingTable({ entradas, resaltarId }: { entradas: RankingEntrada[]; resaltarId?: string }) {
  if (!entradas?.length) {
    return <AppText>Sin datos de ranking todavía.</AppText>;
  }
  return (
    <View style={styles.tabla}>
      {entradas.map((e) => (
        <View key={e.competidorId} style={[styles.fila, e.competidorId === resaltarId ? styles.propia : null]}>
          <AppText variant="bodyStrong">#{e.posicion}</AppText>
          <AppText>{e.competidorId.slice(0, 8)}</AppText>
          <AppText variant="bodyStrong">{e.puntos} pts</AppText>
        </View>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  tabla: { gap: spacing.xs },
  fila: { flexDirection: "row", justifyContent: "space-between", paddingVertical: spacing.xs },
  propia: { backgroundColor: colors.primaryBright + "22", borderRadius: 6, paddingHorizontal: spacing.xs },
});
```

(Verificar tokens reales de `shared/theme`: si `spacing.xs` no existe usar el mínimo real; si `colors.primaryBright` no admite concat alfa, usar un token de fondo suave existente. Ajustar manteniendo estructura.)

- [ ] **Step 2: `TriviaPlayPanel.tsx`**

```tsx
// Gameplay Trivia del participante: pregunta activa + responder una vez + ranking en vivo.
import React, { useCallback, useEffect, useState } from "react";
import { StyleSheet, View } from "react-native";
import { AppText, Button, Card, Notice } from "../../shared/ui";
import { spacing } from "../../shared/theme";
import { getPreguntaActual, responderPregunta, getRankingJuego } from "./gameplayApi.js";
import { Countdown, RankingTable, type RankingEntrada } from "./liveShared";

type Pregunta = {
  preguntaId: string;
  orden: number;
  texto: string;
  opciones: { opcionId: string; texto: string }[];
  fechaActivacion: string;
  tiempoLimiteSegundos: number;
};

type Resultado = { esCorrecta: boolean; puntaje?: number } | null;

type PreguntaResult =
  | { ok: true; pregunta: Pregunta }
  | { ok: false; type: string; message?: string };
type ResponderResult =
  | { ok: true; data: { esCorrecta: boolean; cerroPregunta: boolean; puntaje?: number } }
  | { ok: false; type: string; message?: string };
type RankingResult =
  | { ok: true; ranking: { entradas: RankingEntrada[] } }
  | { ok: false; type: string; message?: string };

type Props = {
  apiBaseUrl: string;
  token: string;
  partidaId: string;
  juegoId: string;
  yaRespondioInicial: boolean;
  refetchSignal: number; // bump = PreguntaActivada/Cerrada del hub
  resetSignal: number; // bump = PreguntaActivada (nueva pregunta → limpiar respondido)
  miSub: string;
};

export function TriviaPlayPanel({
  apiBaseUrl, token, partidaId, juegoId, yaRespondioInicial, refetchSignal, resetSignal, miSub,
}: Props) {
  const [pregunta, setPregunta] = useState<Pregunta | null>(null);
  const [sinPregunta, setSinPregunta] = useState(false);
  const [respondida, setRespondida] = useState(yaRespondioInicial);
  const [resultado, setResultado] = useState<Resultado>(null);
  const [entradas, setEntradas] = useState<RankingEntrada[]>([]);
  const [posting, setPosting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Nueva pregunta activada → limpiar estado de respuesta local.
  useEffect(() => {
    if (resetSignal > 0) {
      setRespondida(false);
      setResultado(null);
    }
  }, [resetSignal]);

  const cargar = useCallback(async () => {
    const r = (await getPreguntaActual(apiBaseUrl, token, partidaId, undefined)) as PreguntaResult;
    if (r.ok) {
      setPregunta(r.pregunta);
      setSinPregunta(false);
    } else if (r.type === "sin_pregunta") {
      setPregunta(null);
      setSinPregunta(true);
    } else {
      setError(r.message ?? "No se pudo cargar la pregunta.");
    }
    const rk = (await getRankingJuego(apiBaseUrl, token, partidaId, juegoId, undefined)) as RankingResult;
    if (rk.ok) setEntradas(rk.ranking.entradas ?? []);
  }, [apiBaseUrl, token, partidaId, juegoId]);

  useEffect(() => {
    void cargar();
  }, [cargar, refetchSignal]);

  async function onResponder(opcionId: string) {
    setPosting(true);
    setError(null);
    const r = (await responderPregunta(apiBaseUrl, token, partidaId, opcionId, undefined)) as ResponderResult;
    setPosting(false);
    if (r.ok) {
      setRespondida(true);
      setResultado({ esCorrecta: r.data.esCorrecta, puntaje: r.data.puntaje });
      return;
    }
    if (r.type === "conflict") {
      // Duplicada (yo o mi equipo) o fuera de tiempo: queda como respondida, sin resultado propio.
      setRespondida(true);
      setError(r.message ?? "La pregunta ya no acepta tu respuesta.");
      return;
    }
    setError(r.message ?? "No se pudo enviar la respuesta.");
  }

  const target = pregunta
    ? new Date(new Date(pregunta.fechaActivacion).getTime() + pregunta.tiempoLimiteSegundos * 1000).toISOString()
    : null;

  return (
    <View style={styles.stack}>
      {error ? <Notice variant="error">{error}</Notice> : null}
      {pregunta ? (
        <Card style={styles.card}>
          <AppText variant="bodyStrong">
            Pregunta {pregunta.orden} — {pregunta.texto}
          </AppText>
          {target ? <Countdown target={target} /> : null}
          {!respondida
            ? pregunta.opciones.map((o) => (
                <Button key={o.opcionId} label={o.texto} disabled={posting} onPress={() => void onResponder(o.opcionId)} />
              ))
            : null}
          {respondida ? (
            <View style={styles.resultado}>
              {resultado ? (
                <Notice variant={resultado.esCorrecta ? "success" : "error"}>
                  {resultado.esCorrecta
                    ? `¡Correcta!${resultado.puntaje != null ? ` +${resultado.puntaje} pts` : ""}`
                    : "Incorrecta."}
                </Notice>
              ) : (
                <Notice variant="info">Tu respuesta ya está registrada.</Notice>
              )}
              <AppText>Esperando el cierre de la pregunta…</AppText>
            </View>
          ) : null}
        </Card>
      ) : null}
      {sinPregunta ? (
        <Card style={styles.card}>
          <AppText>Esperando la siguiente pregunta…</AppText>
        </Card>
      ) : null}
      <Card style={styles.card}>
        <AppText variant="bodyStrong">Ranking del juego</AppText>
        <RankingTable entradas={entradas} resaltarId={miSub} />
      </Card>
    </View>
  );
}

const styles = StyleSheet.create({
  stack: { gap: spacing.lg },
  card: { gap: spacing.sm },
  resultado: { gap: spacing.sm },
});
```

- [ ] **Step 3: `PartidaLiveScreen.tsx`**

```tsx
// Pantalla live del participante: fases por mi-sesion + hub de sesion; monta el panel del juego activo.
import React, { useCallback, useEffect, useRef, useState } from "react";
import { ActivityIndicator, ScrollView, StyleSheet, View } from "react-native";
import { AppText, Button, Card, Notice, ScreenHeader } from "../../shared/ui";
import { colors, spacing } from "../../shared/theme";
import { cargarLive } from "./partidaLiveFlow.js";
import { getRankingConsolidado } from "./gameplayApi.js";
import { crearSesionHub } from "./sesionHub.js";
import { TriviaPlayPanel } from "./TriviaPlayPanel";
import { RankingTable } from "./liveShared";

type JuegoActivo = { juegoId: string; orden: number; tipoJuego: string; estadoJuego: string };

type LiveResult =
  | { ok: true; fase: "sin-participacion" }
  | { ok: true; fase: "lobby" }
  | { ok: true; fase: "iniciada"; juegoActivo: JuegoActivo | null; yaRespondio: boolean }
  | { ok: false; type: string; message?: string };

type ConsolidadoEntrada = {
  posicion: number;
  competidorId: string;
  juegosGanados: number;
  puntosTotales: number;
};
type ConsolidadoResult =
  | { ok: true; ranking: { entradas: ConsolidadoEntrada[] } }
  | { ok: false; type: string; message?: string };

type Fase =
  | { status: "cargando" }
  | { status: "sin-participacion" }
  | { status: "iniciada"; juegoActivo: JuegoActivo | null; yaRespondio: boolean }
  | { status: "finalizada" }
  | { status: "cancelada"; motivo?: string };

type Props = {
  apiBaseUrl: string;
  token: string;
  partidaId: string;
  nombre: string;
  miSub: string;
  onVolverAlPanel: () => void;
};

export function PartidaLiveScreen({ apiBaseUrl, token, partidaId, nombre, miSub, onVolverAlPanel }: Props) {
  const [fase, setFase] = useState<Fase>({ status: "cargando" });
  const [refetchSignal, setRefetchSignal] = useState(0);
  const [resetSignal, setResetSignal] = useState(0);
  const [aviso, setAviso] = useState<string | null>(null);
  const [consolidado, setConsolidado] = useState<ConsolidadoEntrada[] | null>(null);
  const [consolidadoError, setConsolidadoError] = useState(false);

  const load = useCallback(async () => {
    const r = (await cargarLive({ apiBaseUrl, token, partidaId, fetchImpl: undefined })) as LiveResult;
    if (!r.ok) {
      setAviso(r.message ?? "No se pudo cargar la sesión.");
      return;
    }
    if (r.fase === "iniciada") {
      setFase({ status: "iniciada", juegoActivo: r.juegoActivo, yaRespondio: r.yaRespondio });
    } else {
      // "lobby" en el live = la partida aún no inició; tratamos igual que sin-participacion:
      // el usuario llegó antes de tiempo, que vuelva por el flujo normal.
      setFase({ status: "sin-participacion" });
    }
  }, [apiBaseUrl, token, partidaId]);

  useEffect(() => {
    void load();
  }, [load]);

  const cargarConsolidado = useCallback(async () => {
    setConsolidadoError(false);
    const r = (await getRankingConsolidado(apiBaseUrl, token, partidaId, undefined)) as ConsolidadoResult;
    if (r.ok) setConsolidado(r.ranking.entradas ?? []);
    else setConsolidadoError(true);
  }, [apiBaseUrl, token, partidaId]);

  // Hub: señales de pregunta → panel; transiciones → recargar/fases terminales.
  const loadRef = useRef(load);
  loadRef.current = load;
  const cargarConsolidadoRef = useRef(cargarConsolidado);
  cargarConsolidadoRef.current = cargarConsolidado;
  useEffect(() => {
    const hub = crearSesionHub(apiBaseUrl, token);
    hub.on("PreguntaActivada", () => {
      setResetSignal((s) => s + 1);
      setRefetchSignal((s) => s + 1);
    });
    hub.on("PreguntaCerrada", () => setRefetchSignal((s) => s + 1));
    hub.on("JuegoActivado", () => void loadRef.current());
    hub.on("PartidaFinalizada", () => {
      setFase({ status: "finalizada" });
      void cargarConsolidadoRef.current();
    });
    hub.on("PartidaCancelada", (p: { motivo?: string }) =>
      setFase({ status: "cancelada", motivo: p?.motivo })
    );
    hub
      .start()
      .then(() => hub.invoke("SuscribirAPartida", partidaId))
      .catch(() => setAviso("Sin conexión en vivo; los cambios pueden tardar."));
    return () => {
      void hub.stop().catch(() => {});
    };
  }, [apiBaseUrl, token, partidaId]);

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <ScreenHeader title={nombre} subtitle="Partida en vivo" />
      {aviso ? <Notice variant="info">{aviso}</Notice> : null}
      {fase.status === "cargando" ? <ActivityIndicator color={colors.primaryBright} style={styles.spinner} /> : null}
      {fase.status === "sin-participacion" ? (
        <Card style={styles.card}>
          <AppText>No tienes una participación activa en esta partida.</AppText>
          <Button label="Volver a partidas" onPress={onVolverAlPanel} />
        </Card>
      ) : null}
      {fase.status === "iniciada" && fase.juegoActivo?.tipoJuego === "Trivia" ? (
        <TriviaPlayPanel
          key={fase.juegoActivo.juegoId}
          apiBaseUrl={apiBaseUrl}
          token={token}
          partidaId={partidaId}
          juegoId={fase.juegoActivo.juegoId}
          yaRespondioInicial={fase.yaRespondio}
          refetchSignal={refetchSignal}
          resetSignal={resetSignal}
          miSub={miSub}
        />
      ) : null}
      {fase.status === "iniciada" && fase.juegoActivo?.tipoJuego === "BusquedaDelTesoro" ? (
        <Card style={styles.card}>
          <AppText variant="bodyStrong">Búsqueda del tesoro</AppText>
          <AppText>Disponible en la próxima actualización.</AppText>
        </Card>
      ) : null}
      {fase.status === "iniciada" && fase.juegoActivo == null ? (
        <Card style={styles.card}>
          <AppText>Esperando el siguiente juego…</AppText>
        </Card>
      ) : null}
      {fase.status === "finalizada" ? (
        <Card style={styles.card}>
          <AppText variant="bodyStrong">Partida finalizada</AppText>
          {consolidado ? (
            <RankingTable
              entradas={consolidado.map((e) => ({
                posicion: e.posicion,
                competidorId: e.competidorId,
                puntos: e.puntosTotales,
              }))}
              resaltarId={miSub}
            />
          ) : null}
          {consolidadoError ? (
            <View style={styles.retry}>
              <AppText>Consolidado no disponible aún.</AppText>
              <Button label="Reintentar" onPress={() => void cargarConsolidado()} />
            </View>
          ) : null}
          <Button label="Volver a partidas" variant="secondary" onPress={onVolverAlPanel} />
        </Card>
      ) : null}
      {fase.status === "cancelada" ? (
        <Card style={styles.card}>
          <Notice variant="error">
            {fase.motivo ? `Partida cancelada: ${fase.motivo}` : "Partida cancelada."}
          </Notice>
          <Button label="Volver a partidas" onPress={onVolverAlPanel} />
        </Card>
      ) : null}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg },
  content: { padding: spacing.xl, gap: spacing.lg },
  spinner: { marginTop: spacing.lg },
  card: { gap: spacing.sm },
  retry: { gap: spacing.sm },
});
```

- [ ] **Step 4: Container + ruta**

`PartidaLiveScreenContainer.tsx`:

```tsx
import React from "react";
import { Text } from "react-native";
import { RouteProp, useNavigation, useRoute } from "@react-navigation/native";
import type { NativeStackNavigationProp } from "@react-navigation/native-stack";
import { useAuth } from "../../auth/AuthProvider";
import { mobileEnv } from "../../config/env";
import { AppStackParamList } from "../../navigation/types";
import { PartidaLiveScreen } from "./PartidaLiveScreen";

export function PartidaLiveScreenContainer() {
  const { session } = useAuth();
  const navigation = useNavigation<NativeStackNavigationProp<AppStackParamList>>();
  const route = useRoute<RouteProp<AppStackParamList, "PartidaLive">>();

  if (!session) {
    return <Text>Sesion no disponible.</Text>;
  }

  return (
    <PartidaLiveScreen
      apiBaseUrl={mobileEnv.gatewayApiBaseUrl}
      token={session.token}
      partidaId={route.params.partidaId}
      nombre={route.params.nombre}
      miSub={session.user.sub}
      onVolverAlPanel={() => navigation.navigate("PartidasPanel")}
    />
  );
}
```

`navigation/types.ts` — añadir a `AppStackParamList`:

```ts
  PartidaLive: { partidaId: string; nombre: string };
```

`RootNavigator.tsx`:

```tsx
import { PartidaLiveScreenContainer } from "../features/partidas/PartidaLiveScreenContainer";
// dentro del AppStack.Navigator:
<AppStack.Screen name="PartidaLive" component={PartidaLiveScreenContainer} options={{ title: "En vivo" }} />
```

(Verificar que `session.user.sub` existe en `authTypes.ts` — sí: `AuthUser { sub, username, roles }`.)

- [ ] **Step 5: Suite + typecheck**

Run: `cd mobile && npm test && npm run typecheck`
Expected: verde (sin tests nuevos en esta task — la lógica testeable vive en T1/T2; las pantallas siguen el patrón del repo sin tests de render).

- [ ] **Step 6: Commit**

```bash
cd mobile && git add src/features/partidas/liveShared.tsx src/features/partidas/TriviaPlayPanel.tsx src/features/partidas/PartidaLiveScreen.tsx src/features/partidas/PartidaLiveScreenContainer.tsx src/navigation/types.ts src/navigation/RootNavigator.tsx
git commit -m "feat(mobile): PartidaLiveScreen + TriviaPlayPanel gameplay en vivo (bloque 2e1)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 4: Navegación lobby→live + banner del panel

**Files:**
- Modify: `mobile/src/features/partidas/PartidaLobbyScreen.tsx`
- Modify: `mobile/src/features/partidas/PartidaLobbyScreenContainer.tsx`
- Modify: `mobile/src/features/partidas/PartidasPanelScreen.tsx`
- Modify: `mobile/src/features/partidas/PartidasPanelScreenContainer.tsx`

**Interfaces:**
- Consumes: ruta `PartidaLive` (T3); `lobby.estado` que `cargarLobby` ya devuelve (LobbyDto.estado).
- Produces: prop `onIniciada: () => void` en `PartidaLobbyScreen`; prop `onOpenMiSesion: (s: {partidaId: string; estadoPartida: string}) => void` en `PartidasPanelScreen`.

- [ ] **Step 1: Lobby navega al live**

`PartidaLobbyScreen.tsx`:
1. `Props` gana `onIniciada: () => void`.
2. El handler del hub (línea 66) cambia de aviso a navegación:
   ```tsx
   hub.on("PartidaIniciada", () => onIniciadaRef.current());
   ```
   con ref (mismo patrón que `loadRef`, líneas 61-62):
   ```tsx
   const onIniciadaRef = useRef(onIniciada);
   onIniciadaRef.current = onIniciada;
   ```
3. En `load` (tras `setLobby(r.lobby)`): si la sesión ya está iniciada al cargar, salir al live:
   ```tsx
   if (r.lobby.estado === "Iniciada") {
     onIniciadaRef.current();
     return;
   }
   ```
   (colocar el check ANTES de `setLobby`/`setInscrito` para no pintar un lobby muerto).
4. El tipo local `Lobby` ya tiene `estado: string` — sin cambios de tipo.

`PartidaLobbyScreenContainer.tsx`: añadir `useNavigation` (import igual al de `PartidasPanelScreenContainer`) y pasar:
```tsx
onIniciada={() => navigation.replace("PartidaLive", { partidaId: route.params.partidaId, nombre: route.params.nombre })}
```
(`replace` — back no vuelve al lobby muerto.)

- [ ] **Step 2: Banner del panel distingue Lobby/Iniciada**

`PartidasPanelScreen.tsx`:
1. Props: añadir `onOpenMiSesion: (s: { partidaId: string; estadoPartida: string }) => void`.
2. El `Pressable` del banner (líneas 72-78) pasa de `onOpenPartida({partidaId, nombre:"Mi partida"})` a:
   ```tsx
   onPress={() => onOpenMiSesion({ partidaId: miSesion.partidaId, estadoPartida: miSesion.estadoPartida })}
   ```
3. El tipo local `MiSesion` ya incluye `estadoPartida` — sin cambios.

`PartidasPanelScreenContainer.tsx`: pasar la prop nueva:
```tsx
onOpenMiSesion={({ partidaId, estadoPartida }) =>
  estadoPartida === "Iniciada"
    ? navigation.navigate("PartidaLive", { partidaId, nombre: "Mi partida" })
    : navigation.navigate("PartidaLobby", { partidaId, nombre: "Mi partida" })
}
```

- [ ] **Step 3: Suite + typecheck**

Run: `cd mobile && npm test && npm run typecheck`
Expected: verde.

- [ ] **Step 4: Commit**

```bash
cd mobile && git add src/features/partidas/PartidaLobbyScreen.tsx src/features/partidas/PartidaLobbyScreenContainer.tsx src/features/partidas/PartidasPanelScreen.tsx src/features/partidas/PartidasPanelScreenContainer.tsx
git commit -m "feat(mobile): navegacion lobby->live y banner por estado (bloque 2e1)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 5: Fix minor 2d — miembro no-líder en lobby Equipo

**Files:**
- Modify: `mobile/src/features/partidas/partidaLobbyFlow.js` (`cargarLobby` gana `esLider`)
- Modify: `mobile/src/features/partidas/PartidaLobbyScreen.tsx` (rama miembro)
- Test: `mobile/tests/partidaLobbyFlow.test.js` (casos nuevos)

**Interfaces:**
- Consumes: `parseJwtPayload` de `../../auth/tokenClaims.js`; `GET /identity/teams/mine` (`{liderUserId, ...}` · 404 sin equipo).
- Produces: `cargarLobby` devuelve además `esLider: boolean` (solo relevante en modalidad Equipo; `true` en Individual para no alterar el flujo).

- [ ] **Step 1: Tests nuevos (fallan)**

Añadir a `mobile/tests/partidaLobbyFlow.test.js`:

```js
test("cargarLobby en Equipo marca esLider segun teams/mine", async () => {
  // token con payload {"sub":"lider-1"} en base64url (header.payload.sig)
  const token = "x." + Buffer.from(JSON.stringify({ sub: "lider-1" })).toString("base64url") + ".y";
  const make = (liderUserId) => async (url) => {
    if (url.endsWith("/lobby")) {
      return jsonResponse(200, { partidaId: "p1", estado: "Lobby", modalidad: "Equipo", inscritosActivos: 0 });
    }
    if (url.endsWith("/mi-sesion")) {
      return { ok: true, status: 204, json: async () => ({}) };
    }
    if (url.endsWith("/identity/teams/mine")) {
      return jsonResponse(200, { equipoId: "e1", liderUserId });
    }
    throw new Error(`URL inesperada: ${url}`);
  };
  const lider = await cargarLobby({ apiBaseUrl: "http://gw", token, partidaId: "p1", fetchImpl: make("lider-1") });
  assert.equal(lider.esLider, true);
  const miembro = await cargarLobby({ apiBaseUrl: "http://gw", token, partidaId: "p1", fetchImpl: make("otro") });
  assert.equal(miembro.esLider, false);
});

test("cargarLobby Individual no consulta teams/mine y esLider es true", async () => {
  const token = "x." + Buffer.from(JSON.stringify({ sub: "u1" })).toString("base64url") + ".y";
  const urls = [];
  const fetchImpl = async (url) => {
    urls.push(url);
    if (url.endsWith("/lobby")) {
      return jsonResponse(200, { partidaId: "p1", estado: "Lobby", modalidad: "Individual", inscritosActivos: 0 });
    }
    return { ok: true, status: 204, json: async () => ({}) };
  };
  const r = await cargarLobby({ apiBaseUrl: "http://gw", token, partidaId: "p1", fetchImpl });
  assert.equal(r.esLider, true);
  assert.equal(urls.some((u) => u.includes("teams/mine")), false);
});
```

Nota entorno: `node --test` corre en Node puro donde `atob` global existe desde Node 16 — `parseJwtPayload` funciona; `Buffer.from(...).toString("base64url")` genera el payload. Si `atob` faltara en la versión local, el test lo revelará (no mockear `tokenClaims`).

- [ ] **Step 2: Correr → FAIL**

Run: `cd mobile && node --test tests/partidaLobbyFlow.test.js`
Expected: fallan los 2 nuevos (esLider undefined).

- [ ] **Step 3: Implementar en `partidaLobbyFlow.js`**

1. Import nuevo al tope:
   ```js
   import { parseJwtPayload } from "../../auth/tokenClaims.js";
   ```
   (ruta desde `src/features/partidas/` → `../../auth/tokenClaims.js`).
2. En `cargarLobby`, tras computar `inscrito`, añadir:
   ```js
   let esLider = true;
   if (body.modalidad === "Equipo") {
     esLider = false;
     try {
       const tm = await f(`${apiBaseUrl}/identity/teams/mine`, {
         method: "GET",
         headers: { Authorization: `Bearer ${token}` },
       });
       if (tm.ok) {
         const team = await tm.json().catch(() => null);
         const sub = parseJwtPayload(token).sub;
         esLider = team != null && team.liderUserId === sub;
       }
     } catch {
       // sin red hacia identity: tratar como miembro (el backend protege igual con 403)
     }
   }
   return { ok: true, lobby: body, inscrito, esLider };
   ```
   (reemplaza el `return` actual de la línea 27).

- [ ] **Step 4: Rama miembro en `PartidaLobbyScreen.tsx`**

1. `LobbyResult` (línea 29-31) gana el campo: `{ ok: true; lobby: Lobby; inscrito: boolean; esLider: boolean }`.
2. Estado nuevo: `const [esLider, setEsLider] = useState(true);` y en `load`: `setEsLider(r.esLider);`.
3. El bloque de acción dentro del `Card` (líneas 110-111) se condiciona:
   ```tsx
   {lobby.modalidad !== "Equipo" || esLider ? (
     <Button label={labelAccion} onPress={() => void onAccion()} disabled={posting} />
   ) : (
     <AppText>El líder gestiona la preinscripción del equipo.</AppText>
   )}
   <Button label="Recargar" variant="secondary" onPress={() => void load()} disabled={posting} />
   ```

- [ ] **Step 5: PASS + suite + typecheck**

Run: `cd mobile && node --test tests/partidaLobbyFlow.test.js && npm test && npm run typecheck`
Expected: 6/6 en el archivo (4 previos + 2 nuevos); suite verde; tsc limpio.

- [ ] **Step 6: Commit**

```bash
cd mobile && git add src/features/partidas/partidaLobbyFlow.js src/features/partidas/PartidaLobbyScreen.tsx tests/partidaLobbyFlow.test.js
git commit -m "fix(mobile): lobby Equipo distingue lider de miembro (minor 2d, bloque 2e1)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 6: Gate final — E2E vivo + traceability (controller)

**Files:**
- Modify: `docs/04-sdd/traceability-matrix.md` (fila 2e-1)
- Modify: `GUIA-LEVANTAMIENTO.md` (nota gameplay Trivia mobile)

Controller con stack vivo (infra + identity-service + partidas + operaciones-sesion + **puntuaciones** + gateway; tokens PKCE).

- [ ] **Step 1: Suites en HEAD**

Run: `cd mobile && npm test && npm run typecheck`
Expected: verdes.

- [ ] **Step 2: E2E vivo vía gateway :5080**

1. Partida Trivia (2 preguntas) publicada + participante inscrito + iniciada.
2. Participante: `GET /pregunta-actual` 200 — verificar que el payload NO trae campo de opción correcta.
3. Responder correcto: `POST /respuesta` 200 `{esCorrecta:true, puntaje}` → segunda respuesta → **409 duplicada**.
4. `GET /puntuaciones/.../ranking` **con token participante** 200 con sus puntos (policy autenticado confirmada).
5. Cerrar/avanzar (operador) → pregunta 2 → finalizar → participante `GET /ranking-consolidado` 200.
6. Smoke SignalR (node + signalr de `mobile/node_modules`): participante suscrito recibe `PreguntaActivada {fechaLimiteUtc}` y `PreguntaCerrada`.
7. Registrar en ledger. Higiene: partidas E2E finalizadas (mi-sesion 204).

- [ ] **Step 3: Traceability + GUIA + commit**

Fila 2e-1 con hashes T1-T5 verificados (`git cat-file -t`). GUIA: nota "gameplay Trivia mobile requiere puntuaciones en el stack".

```bash
git add docs/04-sdd/traceability-matrix.md GUIA-LEVANTAMIENTO.md
git commit -m "docs(bloque2e1): traceability gameplay trivia mobile" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Self-Review

**Spec coverage:** §1 ruta+navegación → T3 (ruta) + T4 (lobby/banner) ✅ · §2 gameplayApi → T1 ✅ · §3 flow → T2 ✅ · §4 Live+TriviaPanel+consolidado+resaltado → T3 ✅ · §5 fix líder → T5 ✅ · Gate → T6 ✅.

**Placeholder scan:** limpio; las notas "verificar tokens de theme" son verificaciones dirigidas contra archivos concretos del repo.

**Type consistency:** firmas T1 (`getPreguntaActual(apiBaseUrl, token, partidaId, fetchImpl?)` etc.) consumidas idénticas en T3; `cargarLive` shape de T2 = `LiveResult` de T3; `Countdown({target, expiredLabel?})`/`RankingTable({entradas, resaltarId?})` de T3 consumidos en T3 mismo (TriviaPanel + consolidado map a `{posicion, competidorId, puntos}`); `onIniciada`/`onOpenMiSesion` definidos y consumidos en T4; `esLider` producido en T5-flow y consumido en T5-screen; `session.user.sub` verificado en `authTypes.ts`. ✅

**Modelos:** T1-T2 haiku (verbatim) · T3-T5 sonnet · T6 controller. Reviewers sonnet, final opus.
