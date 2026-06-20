import { BdtRankingEntry, BdtRankingSource } from "./bdtRankingTypes";

/**
 * Implementación **mock** de `BdtRankingSource` para la maqueta G4. Datos de ejemplo ya ordenados por la
 * regla BDT (etapas ganadas desc; desempate por tiempo acumulado asc). Sustituible por una
 * `BackendBdtRankingSource` que cumpla la misma interfaz. NO usa puntaje.
 *
 * El guion ilustra el **desempate**: "Ana" y "Tú" ganaron 3 etapas, pero Ana acumuló menos tiempo, así
 * que Ana va 1.ª y Tú 2.º — exactamente cómo manda la regla de BDT.
 */
export function createMockBdtRankingSource(): BdtRankingSource {
  return {
    async load() {
      const filas: BdtRankingEntry[] = [
        { posicion: 1, participante: "Ana", etapasGanadas: 3, tiempoAcumuladoSegundos: 250, esTu: false },
        { posicion: 2, participante: "Tú", etapasGanadas: 3, tiempoAcumuladoSegundos: 312, esTu: true },
        { posicion: 3, participante: "Beto", etapasGanadas: 2, tiempoAcumuladoSegundos: 180, esTu: false },
        { posicion: 4, participante: "Caro", etapasGanadas: 1, tiempoAcumuladoSegundos: 90, esTu: false },
      ];
      return filas;
    },
  };
}
