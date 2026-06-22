/**
 * ── PLANTILLA DE INTEGRACIÓN — Ranking de BDT (maqueta G4) ────────────────────────────────────────
 *
 * La pantalla `BdtRankingScreen` depende **solo** de `BdtRankingSource`. Hoy hay un mock
 * (`mockBdtRankingSource.ts`) para ver/probar el podio sin backend. Cuando exista el ranking real, un
 * agente crea una `BackendBdtRankingSource` que cumpla esta interfaz y la inyecta en el container; la
 * pantalla no cambia.
 *
 * **REGLA DE RANKING BDT (doctrina actual):** es por **puntos acumulados** de las etapas ganadas. Ordena por:
 *   1) mayor **puntaje acumulado** = suma del `Puntaje` de las etapas ganadas, y
 *   2) desempate por **menor tiempo acumulado** de las etapas ganadas (`TiempoAcumuladoEtapasGanadas`).
 * `EtapasGanadas` (número de etapas) es solo dato informativo, no la clave de orden.
 * Conceptos del dominio: `EtapaBDTGanada` (lleva `Puntaje`), `RankingBDTActualizado`.
 * Ver `docs/02-project-context/bdt-ranking-clarification.md`.
 *
 * ⚠ Deuda de migración: esta maqueta aún modela el ranking por etapas ganadas (campos `etapasGanadas` /
 * `tiempoAcumuladoSegundos`). Una `BackendBdtRankingSource` real debe ordenar por puntaje acumulado de
 * etapas ganadas. Ver `docs/02-project-context/documentation-migration-status.md`.
 *
 * Integración real: `load()` → endpoint/evento de ranking de **Puntuaciones** (push `RankingBDTActualizado`
 * vía SignalR a través del gateway, o GET del ranking de la partida). El backend ya entrega el orden;
 * la pantalla solo formatea y muestra.
 */

/** Una fila del ranking BDT, ya ordenada por el backend (puntaje acumulado desc, tiempo asc). */
export interface BdtRankingEntry {
  posicion: number;
  participante: string;
  /** Etapas ganadas (dato informativo bajo la doctrina actual; la clave de orden es el puntaje acumulado). */
  etapasGanadas: number;
  /** Tiempo acumulado, en segundos, de las etapas ganadas (desempate). */
  tiempoAcumuladoSegundos: number;
  /** Marca la fila del propio participante. */
  esTu: boolean;
}

/**
 * Fuente del ranking de una partida BDT. La pantalla consume solo esto; la implementación real la cumple
 * con el endpoint/evento de ranking de Puntuaciones. Ver la cabecera para la regla de orden.
 */
export interface BdtRankingSource {
  /** Devuelve el ranking ya ordenado por la regla BDT (puntaje acumulado de etapas ganadas; desempate por tiempo acumulado). */
  load(): Promise<BdtRankingEntry[]>;
}

/** Formatea segundos a `m:ss` para mostrar el tiempo acumulado en el podio. */
export function formatAccumulatedTime(totalSeconds: number): string {
  const s = Math.max(0, Math.floor(totalSeconds));
  const m = Math.floor(s / 60);
  const rem = s % 60;
  return `${m}:${String(rem).padStart(2, "0")}`;
}

/** Valor mostrado por el `Podium` para BDT: "N etapa(s) · m:ss". NO es puntaje. (maqueta — deuda de migración: el valor real a mostrar debe derivar del puntaje acumulado.) */
export function formatBdtRankingValue(entry: BdtRankingEntry): string {
  const etapas = `${entry.etapasGanadas} ${entry.etapasGanadas === 1 ? "etapa" : "etapas"}`;
  return `${etapas} · ${formatAccumulatedTime(entry.tiempoAcumuladoSegundos)}`;
}
