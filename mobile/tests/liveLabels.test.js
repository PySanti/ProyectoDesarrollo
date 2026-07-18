import test from "node:test";
import assert from "node:assert/strict";
import { etiquetaCompetidor, idsDeCompetidores, motivosDesempateConsolidado } from "../src/features/partidas/liveLabels.js";

const A = "aaaaaaaa-0000-0000-0000-000000000000";
const B = "bbbbbbbb-0000-0000-0000-000000000000";
const EQ = "eeeeeeee-0000-0000-0000-000000000000";

test("la fila propia se rotula Tú", () => {
  const nombreDe = () => "María González";
  assert.strictEqual(etiquetaCompetidor(A, A, nombreDe), "Tú");
});

test("las demás filas usan el nombre resuelto", () => {
  const nombreDe = (id) => (id === B ? "Pedro Ramírez" : "?");
  assert.strictEqual(etiquetaCompetidor(B, A, nombreDe), "Pedro Ramírez");
});

test("sin resaltarId ninguna fila es Tú", () => {
  const nombreDe = () => "Ana";
  assert.strictEqual(etiquetaCompetidor(A, undefined, nombreDe), "Ana");
});

test("idsDeCompetidores reparte por tipoCompetidor", () => {
  const ids = idsDeCompetidores([
    { competidorId: A, tipoCompetidor: "Participante" },
    { competidorId: EQ, tipoCompetidor: "Equipo" },
  ]);
  assert.deepStrictEqual(ids, { participanteIds: [A], equipoIds: [EQ] });
});

test("idsDeCompetidores sin tipoCompetidor asume Participante", () => {
  // Modalidad Individual: el backend manda tipoCompetidor, pero si un payload viejo
  // no lo trae, el competidor es un participante.
  const ids = idsDeCompetidores([{ competidorId: A }]);
  assert.deepStrictEqual(ids, { participanteIds: [A], equipoIds: [] });
});

test("motivosDesempate: mismos puntos y juegos, decide el menor tiempo", () => {
  const motivos = motivosDesempateConsolidado([
    { competidorId: A, puntosTotales: 30, juegosGanados: 1, tiempoTotalMs: 1000 },
    { competidorId: B, puntosTotales: 30, juegosGanados: 1, tiempoTotalMs: 2000 },
  ]);
  assert.deepStrictEqual(motivos, { [A]: "por menor tiempo" });
});

test("motivosDesempate: empate en puntos pero distintos juegos no se marca (juegos es columna visible)", () => {
  // Juegos ganados es el criterio primario y se muestra (🏆): no es un desempate oculto.
  const motivos = motivosDesempateConsolidado([
    { competidorId: A, puntosTotales: 30, juegosGanados: 2, tiempoTotalMs: 5000 },
    { competidorId: B, puntosTotales: 30, juegosGanados: 1, tiempoTotalMs: 1000 },
  ]);
  assert.deepStrictEqual(motivos, {});
});

test("motivosDesempate: puntos distintos no marca nada (no confunde)", () => {
  const motivos = motivosDesempateConsolidado([
    { competidorId: A, puntosTotales: 40, juegosGanados: 1, tiempoTotalMs: 1000 },
    { competidorId: B, puntosTotales: 30, juegosGanados: 1, tiempoTotalMs: 500 },
  ]);
  assert.deepStrictEqual(motivos, {});
});

test("motivosDesempate: empate exacto en los tres criterios no marca nada", () => {
  const motivos = motivosDesempateConsolidado([
    { competidorId: A, puntosTotales: 30, juegosGanados: 1, tiempoTotalMs: 1000 },
    { competidorId: B, puntosTotales: 30, juegosGanados: 1, tiempoTotalMs: 1000 },
  ]);
  assert.deepStrictEqual(motivos, {});
});

const C = "cccccccc-0000-0000-0000-000000000000";

test("motivosDesempate: triple empate con tiempos crecientes marca solo el 1er lugar", () => {
  const motivos = motivosDesempateConsolidado([
    { competidorId: A, puntosTotales: 30, juegosGanados: 1, tiempoTotalMs: 1000 },
    { competidorId: B, puntosTotales: 30, juegosGanados: 1, tiempoTotalMs: 2000 },
    { competidorId: C, puntosTotales: 30, juegosGanados: 1, tiempoTotalMs: 3000 },
  ]);
  assert.deepStrictEqual(motivos, { [A]: "por menor tiempo" });
});

// El bug: 1ro y 2do empatan también en tiempo, solo el 3ro va detrás. El par (1,2)
// no desempata y el par (2,3) sí, así que la lógica vieja marcaba el 2do. Debe ir en el 1ro.
test("motivosDesempate: triple empate con 1ro==2do en tiempo marca el 1er lugar, no el 2do", () => {
  const motivos = motivosDesempateConsolidado([
    { competidorId: A, puntosTotales: 30, juegosGanados: 1, tiempoTotalMs: 1000 },
    { competidorId: B, puntosTotales: 30, juegosGanados: 1, tiempoTotalMs: 1000 },
    { competidorId: C, puntosTotales: 30, juegosGanados: 1, tiempoTotalMs: 3000 },
  ]);
  assert.deepStrictEqual(motivos, { [A]: "por menor tiempo" });
});
