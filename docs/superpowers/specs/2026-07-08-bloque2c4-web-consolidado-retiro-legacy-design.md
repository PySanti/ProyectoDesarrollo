# Bloque 2c-4 — Ranking consolidado en vista terminada + retiro de páginas legacy (web operador)

**Fecha:** 2026-07-08
**Rama:** `feature/bloque-2`
**Servicio dueño:** Puntuaciones (lectura HTTP) · cliente web (operador)
**Precede:** [2c-3 runtime BDT operador]. **Cierra el Bloque 2c.**

## Contexto

La consola de sesión en vivo (`/partidas/:partidaId/sesion`) ya cubre publicar → lobby →
inicio → runtime Trivia (2c-2) → runtime BDT (2c-3). Cuando la partida termina, la vista
`terminada` hoy solo muestra `<p>La partida finalizó.</p>`. Falta el cierre operativo: el
**ranking consolidado** de la partida (RF-45), que Puntuaciones expone por HTTP tras
`PartidaFinalizada`.

Este es el último slice de 2c. Además del consolidado arrastra dos follow-ups de reviews
previos (F-1 build leaflet, F-2 refactor de duplicación) y retira las páginas legacy de
operación que la consola nueva reemplaza.

## Alcance

Solo frontend web. **Sin cambios de contrato, regla de negocio ni HU.** No se tocan
`label`/`id`/`data-testid`/roles ARIA de los que dependen tests existentes; los nuevos
testids se añaden.

### 1. `puntuacionesApi.ts` (extender)

Añadir `getRankingConsolidado(partidaId, tok)`:

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
  partidaId: string, accessToken: string, fetchImpl = fetch,
): Promise<RankingConsolidadoDto>;
```

- Endpoint: `GET /puntuaciones/partidas/{partidaId}/ranking-consolidado`.
- Reusa `PuntuacionesApiError`, `resolveBaseUrl`, `buildAuthHeaders`, `request<T>` del módulo.
- Errores propagados tal cual: `404` (no proyectada), `409` (no `Terminada`). `200` con
  `entradas: []` (terminada sin marcadores) es un caso válido, no error.

### 2. F-2 — `runtimeShared.tsx` (nuevo)

Extraer lo hoy triplicado en `TriviaRuntimePanel`, `BdtRuntimePanel` y el Countdown de la
página:

- `Countdown({ target, testid })` — cuenta regresiva a `target` (Date/ISO); `testid` por prop
  para conservar los ids existentes (`inicio-countdown`, `pregunta-countdown`,
  `etapa-countdown`).
- `RankingView({ ranking })` — tabla posición · competidor (`slice(0,8)`) · puntos · tiempo ·
  unidadesGanadas, con guard `!ranking?.entradas?.length` → mensaje "Sin ranking aún".
- `formatTiempo(ms)` — helper mm:ss compartido.

Reconectar los tres consumidores a este módulo. **Testids y textos visibles intactos** (los
tests de Trivia/BDT existentes deben seguir verdes sin cambios).

### 3. `ConsolidadoPanel.tsx` (nuevo) + vista `terminada`

La vista `terminada` monta `<ConsolidadoPanel partidaId accessToken />` en lugar del `<p>`
actual.

- Al montar: `getRankingConsolidado` con **reintento corto** (~3 intentos, ~1.5s entre
  cada uno) solo ante `404`/`409` — cubre el lag de proyección entre `PartidaFinalizada` y
  que Puntuaciones materialice el consolidado.
- Éxito con entradas → tabla `data-testid="ranking-consolidado"`, columnas:
  posición · competidor (GUID `slice(0,8)`) · juegos ganados · puntos totales ·
  tiempo total (mm:ss vía `formatTiempo`).
- `200` con `entradas: []` → "Sin resultados".
- `404`/`409` persistente tras los reintentos → "Consolidado no disponible aún." + botón
  "Reintentar" (reinicia el ciclo de fetch).
- Testids: `consolidado-panel` (contenedor), `ranking-consolidado` (tabla).
- **No** reusa `RankingView`: columnas distintas (juegos ganados / puntos totales / tiempo
  total vs. posición / puntos / tiempo / unidades). Reutilizar forzaría un componente con dos
  formas; se mantienen separados (YAGNI de abstracción prematura).

### 4. Retiro legacy

Estas páginas quedan reemplazadas por la consola nueva; borrarlas:

- Archivos a eliminar: `features/trivia/TriviaOperationsPage.tsx` (+ su test),
  `features/bdt/PublishedBdtGamesPage.tsx` (+ su test), `api/triviaApi.ts`, `api/bdtApi.ts`
  (+ `bdtApi.test.ts`).
- `App.tsx`: quitar rutas `trivia/operar` y `bdt/partidas` + sus imports.
- `navConfig.tsx`: quitar áreas de nav `trivia` y `bdt` + iconos huérfanos (`Play`, `Flag`).
- `App.test.tsx`: actualizar (quitar aserciones sobre las rutas retiradas).
- `grep` de limpieza: sin referencias colgantes a los símbolos borrados.

Confirmado en slices previos que estos archivos son autocontenidos (solo importados por sus
propias páginas/tests + `App.tsx`/`App.test.tsx`).

### 5. Gate

- `npm run build` (`tsc -b` + `vite build`) — verifica de paso que leaflet bundlea y su CSS
  resuelve (**F-1**, lado build). El pintado real en navegador queda como chequeo manual
  opcional (react-leaflet@4 es React-18-StrictMode-safe; riesgo bajo).
- `npm test` (vitest) verde.
- E2E consolidado con stack vivo: partida con marcadores → finalizar → `GET
  /ranking-consolidado` `200` con entradas; verificar `409` antes de finalizar.
- Traceability: fila 2c-4 + nota "Bloque 2c COMPLETO".

## Sizing (writing-plans lo detalla)

~5 tareas: T1 `puntuacionesApi` (haiku, código verbatim) · T2 F-2 refactor (sonnet, juicio) ·
T3 `ConsolidadoPanel` + terminada (sonnet) · T4 retiro legacy (sonnet) · T5 gate
(controller + E2E). Reviewer sonnet por tarea; review final opus.

## No-objetivos

- No hub SignalR de rankings (SP-4c sigue diferido; consolidado es GET puntual al terminar).
- No resolución de nombres de competidor (contrato entrega solo GUID → `slice(0,8)`).
- No consolidado provisional (contrato: `409` si no `Terminada`).
- No tocar mobile.
