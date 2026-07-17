import test from "node:test";
import assert from "node:assert/strict";
import {
  lineaContextoPartida,
  lineaContextoRendimiento,
} from "../src/features/puntuaciones/historialLabels.js";

test("lineaContextoPartida arma modalidad, puntos, posicion y fecha", () => {
  const linea = lineaContextoPartida({
    modalidad: "Individual",
    puntosTotales: 120,
    posicion: 1,
    fechaFin: "2026-07-15T00:00:00Z",
  });

  assert.match(linea, /^Individual · 120 pts · Posición 1 · /);
});

test("lineaContextoPartida omite la modalidad nula sin separador suelto", () => {
  const linea = lineaContextoPartida({
    modalidad: null,
    puntosTotales: 0,
    posicion: 3,
    fechaFin: null,
  });

  assert.strictEqual(linea, "0 pts · Posición 3");
});

test("lineaContextoPartida omite la fecha nula sin separador colgando", () => {
  const linea = lineaContextoPartida({
    modalidad: "Equipo",
    puntosTotales: 45,
    posicion: 2,
    fechaFin: null,
  });

  assert.strictEqual(linea, "Equipo · 45 pts · Posición 2");
});

test("lineaContextoPartida conserva el cero de puntos", () => {
  // Participacion sin puntuar (slice de 2026-07-15): quien no anoto aparece con 0.
  // Un filter(Boolean) mal puesto lo borraria.
  const linea = lineaContextoPartida({
    modalidad: "Individual",
    puntosTotales: 0,
    posicion: 5,
    fechaFin: null,
  });

  assert.match(linea, /0 pts/);
});

test("lineaContextoRendimiento arma posicion y fecha", () => {
  const linea = lineaContextoRendimiento({ posicion: 1, fechaFin: "2026-07-15T00:00:00Z" });

  assert.match(linea, /^Posición 1 · /);
});

test("lineaContextoRendimiento sin fecha deja solo la posicion", () => {
  const linea = lineaContextoRendimiento({ posicion: 2, fechaFin: null });

  assert.strictEqual(linea, "Posición 2");
});
