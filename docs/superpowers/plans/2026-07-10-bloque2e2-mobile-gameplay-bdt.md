# Bloque 2e-2 — Mobile gameplay BDT Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Gameplay BDT completo del participante mobile (QR cámara/galería con reintentos ilimitados, pistas en vivo, geolocalización ~2s al operador) reemplazando el placeholder de 2e-1, + 2 minors diferidos de 2e-1.

**Architecture:** Espejo del patrón 2e-1: api result-object `.js` testeable con `node --test` → panel `.tsx` hermano de `TriviaPlayPanel` montado por `PartidaLiveScreen`, que gana handlers hub BDT (`EtapaActivada`/`EtapaCerrada`/`EtapaGanada`/`PistaEnviada`), estado de pistas y un efecto de geolocalización (`expo-location` watch → `hub.invoke("EnviarUbicacion")` vía `hubRef`). Huérfanos `permissions/bdt*` reusados (picker adaptado a base64). Sin cambios backend/contrato.

**Tech Stack:** React Native + Expo SDK 54, `expo-image-picker` ~17 y `expo-location` ~19 (ya instalados), `@microsoft/signalr@^8`, `node --test` + `tsc --noEmit`.

## Global Constraints

- Spec: `docs/superpowers/specs/2026-07-10-bloque2e2-mobile-gameplay-bdt-design.md`.
- Solo `mobile/` (+docs en T6). PROHIBIDO tocar backend, gateway, frontend web o `contracts/`.
- Api modules `.js` con patrón result-object `{ok,...}`/`{ok:false,type,message}` sin throws, `fetchImpl = fetch` inyectable como último parámetro.
- Shared-ui API real: `Button label=` (NO children) + `variant`/`disabled`/`onPress`; `Notice variant=`; `AppText variant="bodyStrong"`; `Card style=`. Theme: `colors`, `spacing` de `../../shared/theme`.
- Los `.tsx` puentean `.js` sin tipos con tipos locales + cast por llamada (idioma documentado en `PartidaLobbyScreen.tsx:27-29`).
- Gates por tarea: `cd mobile && npm test` (baseline pre-T1: **70/70**) y `npm run typecheck` (limpio). Node ≥ 20.19.4.
- Commits con trailer: `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- PROHIBIDO a implementers: `git stash/reset/checkout/restore/clean`. Solo `git add <paths exactos>` + `git commit`.

---

### Task 1: `gameplayApi.js` — `getEtapaActual` + `validarTesoro` + tests

**Files:**
- Modify: `mobile/src/features/partidas/gameplayApi.js` (append al final; NO tocar las 4 funciones existentes ni el helper `get()`)
- Test: `mobile/tests/gameplayApi.test.js` (append 2 tests; NO tocar los existentes)

**Interfaces:**
- Consumes: helper interno `get(apiBaseUrl, token, path, fetchImpl)` y `mapCommonError`/`networkError` ya presentes en el archivo.
- Produces: `getEtapaActual(apiBaseUrl, token, partidaId, fetchImpl = fetch)` → `{ok: true, etapa}` | `{ok: false, type: "sin_etapa", message}` (409) | error mapeado. `validarTesoro(apiBaseUrl, token, partidaId, imagenBase64, fetchImpl = fetch)` → `{ok: true, data}` (200 SIEMPRE es ok, incluso `resultado: "Invalido"`) | error mapeado. T3 los consume.

- [ ] **Step 1: Escribir los tests que fallan** — append a `mobile/tests/gameplayApi.test.js`:

```js
test("getEtapaActual 200 devuelve etapa y 409 devuelve sin_etapa", async () => {
  const okImpl = async (url, init) => {
    assert.equal(url, "http://gw/operaciones-sesion/partidas/p1/etapa-actual");
    assert.equal(init.headers.Authorization, "Bearer tok");
    return jsonResponse(200, { etapaId: "e1", orden: 1, areaBusqueda: "Plaza central" });
  };
  const r1 = await getEtapaActual("http://gw", "tok", "p1", okImpl);
  assert.equal(r1.ok, true);
  assert.equal(r1.etapa.etapaId, "e1");

  const sinImpl = async () => jsonResponse(409, { message: "sin etapa activa" });
  const r2 = await getEtapaActual("http://gw", "tok", "p1", sinImpl);
  assert.equal(r2.ok, false);
  assert.equal(r2.type, "sin_etapa");
});

test("validarTesoro POST body imagenBase64; Invalido es 200 ok:true", async () => {
  const calls = [];
  const impl = async (url, init) => {
    calls.push({ url, method: init.method, body: init.body });
    return jsonResponse(200, { resultado: "Invalido", gano: false, cerroEtapa: false, puntaje: null });
  };
  const r = await validarTesoro("http://gw", "tok", "p1", "QkFTRTY0", impl);
  assert.equal(r.ok, true);
  assert.equal(r.data.resultado, "Invalido");
  assert.deepEqual(calls, [{
    url: "http://gw/operaciones-sesion/partidas/p1/etapa-actual/tesoro",
    method: "POST",
    body: JSON.stringify({ imagenBase64: "QkFTRTY0" }),
  }]);

  const errImpl = async () => jsonResponse(403, { message: "No inscrito." });
  const r2 = await validarTesoro("http://gw", "tok", "p1", "QkFTRTY0", errImpl);
  assert.equal(r2.ok, false);
});
```

Y añadir `getEtapaActual, validarTesoro` al import del top del archivo de test.

- [ ] **Step 2: Verificar que fallan** — Run: `cd mobile && node --test tests/gameplayApi.test.js`. Expected: FAIL (`getEtapaActual` is not exported / not a function).

- [ ] **Step 3: Implementar** — append a `mobile/src/features/partidas/gameplayApi.js`:

```js
export async function getEtapaActual(apiBaseUrl, token, partidaId, fetchImpl = fetch) {
  const { response, body, error } = await get(
    apiBaseUrl, token, `/operaciones-sesion/partidas/${partidaId}/etapa-actual`, fetchImpl,
  );
  if (error) return error;
  if (response.status === 409) {
    return { ok: false, type: "sin_etapa", message: body?.message || "Sin etapa activa." };
  }
  if (!response.ok) return mapCommonError(response.status, body);
  return { ok: true, etapa: body };
}

export async function validarTesoro(apiBaseUrl, token, partidaId, imagenBase64, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(
      `${apiBaseUrl}/operaciones-sesion/partidas/${partidaId}/etapa-actual/tesoro`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
        body: JSON.stringify({ imagenBase64 }),
      },
    );
  } catch {
    return networkError();
  }
  const body = await response.json().catch(() => null);
  if (!response.ok) return mapCommonError(response.status, body);
  return { ok: true, data: body };
}
```

- [ ] **Step 4: Verificar verde** — Run: `cd mobile && node --test tests/gameplayApi.test.js` (todos PASS) y `npm test` (72/72) y `npm run typecheck` (limpio).

- [ ] **Step 5: Commit**

```bash
git add mobile/src/features/partidas/gameplayApi.js mobile/tests/gameplayApi.test.js
git commit -m "feat(mobile): gameplayApi etapa-actual y validar tesoro BDT (bloque 2e2)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 2: `bdtTreasureImagePicker.js` — base64 + tests

**Files:**
- Modify: `mobile/src/permissions/bdtTreasureImagePicker.js`
- Create: `mobile/tests/bdtTreasureImagePicker.test.js`

**Interfaces:**
- Consumes: nada de otras tareas (módulo huérfano preexistente; `requestBdtTreasureImagePermission` y `pickBdtTreasureImage(loader?, source?)` ya existen con result-objects `{granted, unavailable}` / `{cancelled: true}` / `{image}`).
- Produces: `pickBdtTreasureImage` pasa `base64: true` a `launchCameraAsync`/`launchImageLibraryAsync` y el resultado gana `image.base64` (string base64 sin prefijo data-URI, como lo entrega expo). T3 consume `image.base64`.

- [ ] **Step 1: Escribir test que falla** — crear `mobile/tests/bdtTreasureImagePicker.test.js`:

```js
import test from "node:test";
import assert from "node:assert/strict";
import { pickBdtTreasureImage } from "../src/permissions/bdtTreasureImagePicker.js";

test("pickBdtTreasureImage pide base64 y lo expone en el resultado", async () => {
  const opciones = [];
  const fakePicker = {
    MediaTypeOptions: { Images: "Images" },
    launchImageLibraryAsync: async (opts) => {
      opciones.push(opts);
      return { canceled: false, assets: [{ uri: "file:///a/foto.jpg", base64: "QkFTRTY0", mimeType: "image/jpeg" }] };
    },
  };
  const r = await pickBdtTreasureImage(async () => fakePicker, "library");
  assert.equal(opciones[0].base64, true);
  assert.equal(r.image.base64, "QkFTRTY0");
  assert.equal(r.image.uri, "file:///a/foto.jpg");
});

test("pickBdtTreasureImage camera usa launchCameraAsync y cancelado devuelve cancelled", async () => {
  let usoCamara = false;
  const fakePicker = {
    MediaTypeOptions: { Images: "Images" },
    launchCameraAsync: async (opts) => {
      usoCamara = true;
      assert.equal(opts.base64, true);
      return { canceled: true };
    },
    launchImageLibraryAsync: async () => {
      throw new Error("no debía usar galería");
    },
  };
  const r = await pickBdtTreasureImage(async () => fakePicker, "camera");
  assert.equal(usoCamara, true);
  assert.equal(r.cancelled, true);
});
```

- [ ] **Step 2: Verificar que falla** — Run: `cd mobile && node --test tests/bdtTreasureImagePicker.test.js`. Expected: FAIL (`opciones[0].base64` undefined ≠ true).

- [ ] **Step 3: Implementar** — en `mobile/src/permissions/bdtTreasureImagePicker.js`, dentro de `pickBdtTreasureImage`, cambiar la llamada a `launch` y el objeto retornado:

```js
  const result = await launch({
    mediaTypes: ImagePicker.MediaTypeOptions?.Images ?? "Images",
    quality: 0.9,
    base64: true,
  });
```

y en el `return` final añadir `base64` al objeto `image`:

```js
  return {
    image: {
      uri: asset.uri,
      base64: asset.base64,
      name: asset.fileName || buildFileName(asset.uri, asset.mimeType),
      type: asset.mimeType || inferContentType(asset.uri),
      size: asset.fileSize,
    },
  };
```

Nada más cambia en el archivo (permisos, fallbacks y helpers quedan intactos).

- [ ] **Step 4: Verificar verde** — Run: `cd mobile && node --test tests/bdtTreasureImagePicker.test.js` (2/2) y `npm test` (74/74) y `npm run typecheck` (limpio).

- [ ] **Step 5: Commit**

```bash
git add mobile/src/permissions/bdtTreasureImagePicker.js mobile/tests/bdtTreasureImagePicker.test.js
git commit -m "feat(mobile): picker de tesoro entrega base64 para validacion QR (bloque 2e2)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 3: `BdtPlayPanel.tsx`

**Files:**
- Create: `mobile/src/features/partidas/BdtPlayPanel.tsx`

**Interfaces:**
- Consumes: `getEtapaActual`/`validarTesoro` (T1, shapes en ese task), `getRankingJuego` (existente: `(apiBaseUrl, token, partidaId, juegoId, fetchImpl)` → `{ok: true, ranking: {entradas}}`), `pickBdtTreasureImage`/`requestBdtTreasureImagePermission` (T2), `Countdown`/`RankingTable`/`RankingEntrada` de `./liveShared`.
- Produces: `<BdtPlayPanel apiBaseUrl token partidaId juegoId refetchSignal resetSignal miSub pistas />` con `pistas: {texto: string, timestampUtc: string}[]`. T4 lo monta.

- [ ] **Step 1: Crear el componente** — `mobile/src/features/partidas/BdtPlayPanel.tsx` completo:

```tsx
// Gameplay BDT del participante: etapa activa + tesoro QR (reintentos ilimitados) + pistas + ranking.
import React, { useCallback, useEffect, useState } from "react";
import { StyleSheet, View } from "react-native";
import { AppText, Button, Card, Notice } from "../../shared/ui";
import { spacing } from "../../shared/theme";
import { getEtapaActual, validarTesoro, getRankingJuego } from "./gameplayApi.js";
import { pickBdtTreasureImage, requestBdtTreasureImagePermission } from "../../permissions/bdtTreasureImagePicker.js";
import { Countdown, RankingTable, type RankingEntrada } from "./liveShared";

type Etapa = {
  etapaId: string;
  orden: number;
  areaBusqueda: string;
  fechaActivacion: string;
  tiempoLimiteSegundos: number;
};

export type Pista = { texto: string; timestampUtc: string };

type EtapaResult =
  | { ok: true; etapa: Etapa }
  | { ok: false; type: string; message?: string };
type TesoroResult =
  | { ok: true; data: { resultado: string; gano: boolean; cerroEtapa: boolean; puntaje?: number | null } }
  | { ok: false; type: string; message?: string };
type RankingResult =
  | { ok: true; ranking: { entradas: RankingEntrada[] } }
  | { ok: false; type: string; message?: string };
type PermisoResult = { granted: boolean; unavailable: boolean };
type PickResult = { cancelled: true } | { image: { base64?: string } };

type Aviso = { variant: "info" | "error" | "success"; texto: string } | null;

type Props = {
  apiBaseUrl: string;
  token: string;
  partidaId: string;
  juegoId: string;
  refetchSignal: number; // bump = EtapaActivada/Cerrada/Ganada del hub
  resetSignal: number; // bump = EtapaActivada (nueva etapa → limpiar aviso del intento)
  miSub: string;
  pistas: Pista[];
};

const MOTIVOS: Record<string, string> = {
  Invalido: "El QR no corresponde a esta etapa. Inténtalo de nuevo.",
  NoLegible: "No se pudo leer un QR en la imagen. Inténtalo de nuevo.",
  NoCorrespondeEtapaActiva: "Ese QR no es el de la etapa activa. Inténtalo de nuevo.",
};

export function BdtPlayPanel({
  apiBaseUrl, token, partidaId, juegoId, refetchSignal, resetSignal, miSub, pistas,
}: Props) {
  const [etapa, setEtapa] = useState<Etapa | null>(null);
  const [sinEtapa, setSinEtapa] = useState(false);
  const [entradas, setEntradas] = useState<RankingEntrada[]>([]);
  const [posting, setPosting] = useState(false);
  const [aviso, setAviso] = useState<Aviso>(null);

  // Nueva etapa activada → limpiar el aviso del intento anterior.
  useEffect(() => {
    if (resetSignal > 0) setAviso(null);
  }, [resetSignal]);

  const cargar = useCallback(async () => {
    const r = (await getEtapaActual(apiBaseUrl, token, partidaId, undefined)) as EtapaResult;
    if (r.ok) {
      setEtapa(r.etapa);
      setSinEtapa(false);
    } else if (r.type === "sin_etapa") {
      setEtapa(null);
      setSinEtapa(true);
    } else {
      setAviso({ variant: "error", texto: r.message ?? "No se pudo cargar la etapa." });
    }
    const rk = (await getRankingJuego(apiBaseUrl, token, partidaId, juegoId, undefined)) as RankingResult;
    if (rk.ok) setEntradas(rk.ranking.entradas ?? []);
  }, [apiBaseUrl, token, partidaId, juegoId]);

  useEffect(() => {
    void cargar();
  }, [cargar, refetchSignal]);

  async function onSubir(source: "camera" | "library") {
    setAviso(null);
    const permiso = (await requestBdtTreasureImagePermission()) as PermisoResult;
    if (!permiso.granted) {
      setAviso({
        variant: "info",
        texto: permiso.unavailable
          ? "La cámara/galería no está disponible en este dispositivo."
          : "Se necesita permiso de cámara o galería para subir el tesoro.",
      });
      return;
    }
    const pick = (await pickBdtTreasureImage(undefined, source)) as PickResult;
    if ("cancelled" in pick || !pick.image.base64) {
      return;
    }
    setPosting(true);
    const r = (await validarTesoro(apiBaseUrl, token, partidaId, pick.image.base64, undefined)) as TesoroResult;
    setPosting(false);
    if (!r.ok) {
      setAviso({ variant: "error", texto: r.message ?? "No se pudo validar el tesoro." });
      return;
    }
    if (r.data.gano) {
      setAviso({
        variant: "success",
        texto: `¡Etapa ganada!${r.data.puntaje != null ? ` +${r.data.puntaje} pts` : ""}`,
      });
      return;
    }
    setAviso({ variant: "error", texto: MOTIVOS[r.data.resultado] ?? "Validación fallida. Inténtalo de nuevo." });
  }

  const target = etapa
    ? new Date(new Date(etapa.fechaActivacion).getTime() + etapa.tiempoLimiteSegundos * 1000).toISOString()
    : null;

  return (
    <View style={styles.stack}>
      {aviso ? <Notice variant={aviso.variant}>{aviso.texto}</Notice> : null}
      {etapa ? (
        <Card style={styles.card}>
          <AppText variant="bodyStrong">Etapa {etapa.orden} — Búsqueda del tesoro</AppText>
          <AppText>Zona de búsqueda: {etapa.areaBusqueda}</AppText>
          {target ? <Countdown target={target} /> : null}
          <Button label="Subir QR con la cámara" disabled={posting} onPress={() => void onSubir("camera")} />
          <Button label="Subir QR desde la galería" variant="secondary" disabled={posting} onPress={() => void onSubir("library")} />
        </Card>
      ) : null}
      {sinEtapa ? (
        <Card style={styles.card}>
          <AppText>Esperando la siguiente etapa…</AppText>
        </Card>
      ) : null}
      {pistas.length > 0 ? (
        <Card style={styles.card}>
          <AppText variant="bodyStrong">Pistas recibidas</AppText>
          {pistas.map((p, i) => (
            <Notice key={`${p.timestampUtc}-${i}`} variant="info">{p.texto}</Notice>
          ))}
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
});
```

- [ ] **Step 2: Verificar gates** — Run: `cd mobile && npm run typecheck` (limpio) y `npm test` (74/74 — el componente no tiene test propio; la lógica de red vive en T1, estrategia del repo: tests de flows/apis, no de componentes).

- [ ] **Step 3: Commit**

```bash
git add mobile/src/features/partidas/BdtPlayPanel.tsx
git commit -m "feat(mobile): BdtPlayPanel etapa + tesoro QR + pistas + ranking (bloque 2e2)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 4: `PartidaLiveScreen` — rama BDT + hub + pistas + geolocalización

**Files:**
- Modify: `mobile/src/features/partidas/PartidaLiveScreen.tsx`

**Interfaces:**
- Consumes: `BdtPlayPanel` + tipo `Pista` (T3), `requestBdtGeolocationPermission` de `../../permissions/bdtGeolocationPermission.js` (existente, sin cambios: `() => Promise<{granted, unavailable}>`), hub existente de la pantalla.
- Produces: nada nuevo hacia otras tareas (pantalla hoja).

- [ ] **Step 1: Imports y estado** — en `PartidaLiveScreen.tsx` añadir a los imports:

```tsx
import { BdtPlayPanel, type Pista } from "./BdtPlayPanel";
import { requestBdtGeolocationPermission } from "../../permissions/bdtGeolocationPermission.js";
```

y junto a los `useState` existentes:

```tsx
  const [pistas, setPistas] = useState<Pista[]>([]);
  const [avisoGeo, setAvisoGeo] = useState<string | null>(null);
  const hubRef = useRef<ReturnType<typeof crearSesionHub> | null>(null);
```

(`crearSesionHub` viene de un `.js` sin tipos → `ReturnType` da `any`; aceptable, el idioma del feature ya castea en los bordes.)

- [ ] **Step 2: Handlers hub BDT + hubRef** — dentro del `useEffect` del hub, después de `hub.on("PreguntaCerrada", ...)` añadir:

```tsx
    hub.on("EtapaActivada", () => {
      setResetSignal((s) => s + 1);
      setRefetchSignal((s) => s + 1);
    });
    hub.on("EtapaCerrada", () => setRefetchSignal((s) => s + 1));
    hub.on("EtapaGanada", () => setRefetchSignal((s) => s + 1));
    hub.on("PistaEnviada", (p: { texto?: string; timestampUtc?: string }) => {
      if (p?.texto) {
        setPistas((prev) => [{ texto: p.texto as string, timestampUtc: p.timestampUtc ?? "" }, ...prev]);
      }
    });
```

En el handler existente de `JuegoActivado` añadir la limpieza de pistas (las pistas son de la etapa/juego en curso):

```tsx
    hub.on("JuegoActivado", () => {
      setPistas([]);
      void loadRef.current();
    });
```

Y publicar el hub en la ref: tras `const hub = crearSesionHub(apiBaseUrl, token);` añadir `hubRef.current = hub;`, y en el cleanup, antes de `void hub.stop()...`:

```tsx
      hubRef.current = null;
```

- [ ] **Step 3: Efecto de geolocalización** — nuevo `useEffect` después del efecto del hub. La geolocalización es obligatoria mientras el juego activo es BDT (SRS); envío best-effort cada ~2s vía el hub ya suscrito:

```tsx
  const esBdtActivo = fase.status === "iniciada" && fase.juegoActivo?.tipoJuego === "BusquedaDelTesoro";
  useEffect(() => {
    if (!esBdtActivo) return;
    let cancelado = false;
    let watcher: { remove: () => void } | null = null;
    (async () => {
      const permiso = (await requestBdtGeolocationPermission()) as { granted: boolean; unavailable: boolean };
      if (cancelado) return;
      if (!permiso.granted) {
        setAvisoGeo("La geolocalización es obligatoria en Búsqueda del Tesoro. Actívala para continuar.");
        return;
      }
      setAvisoGeo(null);
      const Location = await import("expo-location");
      if (cancelado) return;
      watcher = await Location.watchPositionAsync(
        { accuracy: Location.Accuracy.Balanced, timeInterval: 2000, distanceInterval: 0 },
        (pos) => {
          void hubRef.current?.invoke("EnviarUbicacion", pos.coords.latitude, pos.coords.longitude).catch(() => {});
        },
      );
      if (cancelado) watcher.remove();
    })().catch(() => setAvisoGeo("No se pudo iniciar la geolocalización."));
    return () => {
      cancelado = true;
      watcher?.remove();
    };
  }, [esBdtActivo]);
```

- [ ] **Step 4: Render** — reemplazar el bloque placeholder BDT completo (el `<Card>` con "Disponible en la próxima actualización") por:

```tsx
      {fase.status === "iniciada" && fase.juegoActivo?.tipoJuego === "BusquedaDelTesoro" ? (
        <BdtPlayPanel
          key={fase.juegoActivo.juegoId}
          apiBaseUrl={apiBaseUrl}
          token={token}
          partidaId={partidaId}
          juegoId={fase.juegoActivo.juegoId}
          refetchSignal={refetchSignal}
          resetSignal={resetSignal}
          miSub={miSub}
          pistas={pistas}
        />
      ) : null}
```

y bajo el `{aviso ? ... : null}` existente añadir:

```tsx
      {avisoGeo ? <Notice variant="error">{avisoGeo}</Notice> : null}
```

- [ ] **Step 5: Verificar gates** — Run: `cd mobile && npm run typecheck` (limpio) y `npm test` (74/74).

- [ ] **Step 6: Commit**

```bash
git add mobile/src/features/partidas/PartidaLiveScreen.tsx
git commit -m "feat(mobile): live BDT con hub etapas, pistas y geolocalizacion (bloque 2e2)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 5: Minors 2e-1 — columna `juegosGanados` + limpieza de error en reset

**Files:**
- Modify: `mobile/src/features/partidas/liveShared.tsx`
- Modify: `mobile/src/features/partidas/PartidaLiveScreen.tsx` (map del consolidado)
- Modify: `mobile/src/features/partidas/TriviaPlayPanel.tsx` (reset effect)

**Interfaces:**
- Consumes: `RankingTable`/`RankingEntrada` (liveShared), map de consolidado en `PartidaLiveScreen` (fase finalizada), reset effect de `TriviaPlayPanel`.
- Produces: `RankingEntrada` gana `juegosGanados?: number` (opcional — las entradas de ranking de juego no lo traen y nada cambia para ellas).

- [ ] **Step 1: `liveShared.tsx`** — `RankingEntrada` gana el campo opcional y la fila lo muestra solo si viene:

```tsx
export type RankingEntrada = {
  posicion: number;
  competidorId: string;
  puntos: number;
  juegosGanados?: number;
};
```

y en el render de la fila, entre el competidor y los puntos:

```tsx
          <AppText>{e.competidorId.slice(0, 8)}</AppText>
          {e.juegosGanados != null ? <AppText>{e.juegosGanados} 🏆</AppText> : null}
          <AppText variant="bodyStrong">{e.puntos} pts</AppText>
```

- [ ] **Step 2: `PartidaLiveScreen.tsx`** — el map del consolidado (fase finalizada) pasa el campo:

```tsx
              entradas={consolidado.map((e) => ({
                posicion: e.posicion,
                competidorId: e.competidorId,
                puntos: e.puntosTotales,
                juegosGanados: e.juegosGanados,
              }))}
```

- [ ] **Step 3: `TriviaPlayPanel.tsx`** — el reset effect limpia también el error de la pregunta anterior:

```tsx
  useEffect(() => {
    if (resetSignal > 0) {
      setRespondida(false);
      setResultado(null);
      setError(null);
    }
  }, [resetSignal]);
```

- [ ] **Step 4: Verificar gates** — Run: `cd mobile && npm run typecheck` (limpio) y `npm test` (74/74).

- [ ] **Step 5: Commit**

```bash
git add mobile/src/features/partidas/liveShared.tsx mobile/src/features/partidas/PartidaLiveScreen.tsx mobile/src/features/partidas/TriviaPlayPanel.tsx
git commit -m "fix(mobile): consolidado muestra juegos ganados y reset limpia error (minors 2e-1)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 6: Gate E2E + traceability (controller — NO subagente)

**Files:**
- Modify: `docs/04-sdd/traceability-matrix.md` (fila 2e-2 tras la 2e-1)
- Modify: `GUIA-LEVANTAMIENTO.md` (nota cierre Bloque 2e)

**Interfaces:**
- Consumes: todo lo anterior a HEAD; stack completo (infra + identity :5000 + partidas :5010 + operaciones :5020 + puntuaciones :5030 + gateway :5080); script `get-token.sh` PKCE (recrear en scratchpad si purgado — usar `base64 -w0` en el verifier); smoke SignalR vía `createRequire` CJS de `mobile/node_modules/@microsoft/signalr`.
- Produces: cierre del bloque (fila traceability + nota GUIA + ledger).

- [ ] **Step 1: Suites en HEAD** — `cd mobile && npm test` (74/74) + `npm run typecheck` (limpio).
- [ ] **Step 2: Stack + tokens** — levantar infra + 4 servicios + gateway; tokens operador y participante.
- [ ] **Step 3: QR de prueba** — generar PNG con el texto esperado (p. ej. `python3 -c "import qrcode; qrcode.make('TESORO-E2E-1').save('/tmp/.../qr1.png')"` — si `qrcode` no está instalado, `pip install --user qrcode[pil]`; base64: `base64 -w0 qr1.png`). QR "incorrecto": PNG de otro texto (`TESORO-OTRO`).
- [ ] **Step 4: E2E HTTP** — crear partida BDT (POST `{gw}/partidas` Individual Manual + POST `{gw}/partidas/{id}/juegos/bdt` con `{orden:1, areaBusqueda:"Plaza E2E", etapas:[{orden:1, codigoQREsperado:"TESORO-E2E-1", puntaje:50, tiempoLimiteSegundos:120},{orden:2, codigoQREsperado:"TESORO-E2E-2", puntaje:30, tiempoLimiteSegundos:120}]}`) → publicar 201 → inscribir participante 201 → iniciar → `GET /etapa-actual` participante 200 **sin `codigoQREsperado`** (grep) → tesoro con QR de otro texto 200 `{resultado:"Invalido", gano:false}` → reintento con el mismo QR incorrecto 200 de nuevo (sin 409 — reintentos ilimitados) → QR correcto 200 `{resultado:"Valido", gano:true, puntaje:50}` → ranking juego token participante 200 → avanzar etapa (operador POST `/etapa-actual/avance`) → ganar etapa 2 con su QR → finalizar juego → consolidado 200 → higiene mi-sesión 204.
- [ ] **Step 5: Smoke SignalR** — cliente participante suscrito ANTES de iniciar captura `EtapaActivada {fechaLimiteUtc}` / `EtapaGanada` / `EtapaCerrada` / `PistaEnviada {texto}` (operador envía POST `/pistas` `{participanteDestinoId, texto}` con el sub del participante) / `PartidaFinalizada`. Smoke geoloc: el cliente participante invoca `EnviarUbicacion(10.5, -66.9)` y un segundo cliente **operador** suscrito recibe `UbicacionActualizada` con esas coordenadas.
- [ ] **Step 6: Docs** — fila 2e-2 en traceability (patrón de la fila 2e-1: alcance, servicios, spec/plan, contratos, evidencia E2E con shapes reales, commits T1-T5, diferidos) + GUIA: nota "Desde el Bloque 2e-2 el participante mobile juega BDT (QR, pistas, geolocalización) — Bloque 2e completo".
- [ ] **Step 7: Commit**

```bash
git add docs/04-sdd/traceability-matrix.md GUIA-LEVANTAMIENTO.md
git commit -m "docs(bloque2e2): traceability fila 2e-2 + nota GUIA cierre bloque 2e" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```
