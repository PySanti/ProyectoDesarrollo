import { describe, expect, it } from "vitest";
import { etiquetaJuego } from "./juegoLabels";

const JUEGO = "abcdef12-0000-0000-0000-000000000000";

describe("etiquetaJuego", () => {
  it("compone orden y tipo para Trivia", () => {
    expect(etiquetaJuego(1, "Trivia", JUEGO)).toBe("Juego 1 · Trivia");
  });

  it("traduce BusquedaDelTesoro a texto legible", () => {
    expect(etiquetaJuego(2, "BusquedaDelTesoro", JUEGO)).toBe("Juego 2 · Búsqueda del Tesoro");
  });

  it("sin juego devuelve raya: el evento es de partida", () => {
    expect(etiquetaJuego(null, null, null)).toBe("—");
  });

  it("con juegoId pero sin orden cae al GUID corto, no a raya", () => {
    // Hay un juego pero no se sabe cuál (lag de proyección): pintar "—" mentiría.
    expect(etiquetaJuego(null, null, JUEGO)).toBe("abcdef12");
  });

  it("tipo desconocido se pinta tal cual, sin romper", () => {
    expect(etiquetaJuego(3, "AlgoNuevo", JUEGO)).toBe("Juego 3 · AlgoNuevo");
  });

  it("orden sin tipo pinta solo el orden", () => {
    expect(etiquetaJuego(4, null, JUEGO)).toBe("Juego 4");
  });
});
