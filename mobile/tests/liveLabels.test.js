import test from "node:test";
import assert from "node:assert/strict";
import { etiquetaCompetidor, idsDeCompetidores } from "../src/features/partidas/liveLabels.js";

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
