import test from "node:test";
import assert from "node:assert/strict";
import { nombreCorto, trocear } from "../src/features/shared/useNombres.js";

const A = "aaaaaaaa-0000-0000-0000-000000000000";
const EQ = "eeeeeeee-0000-0000-0000-000000000000";

test("nombreCorto recorta el GUID a 8 caracteres", () => {
  assert.strictEqual(nombreCorto(A), "aaaaaaaa");
});

test("trocear reparte en lotes de 200 contando ambas listas sumadas", () => {
  const muchos = Array.from({ length: 250 }, (_, i) => `${String(i).padStart(8, "0")}-x`);
  const lotes = trocear(muchos, [EQ]);
  assert.strictEqual(lotes.length, 2);
  assert.strictEqual(lotes[0].participanteIds.length, 200);
  assert.strictEqual(lotes[0].equipoIds.length, 0);
  assert.strictEqual(lotes[1].participanteIds.length, 50);
  assert.deepStrictEqual(lotes[1].equipoIds, [EQ]);
});

test("trocear con listas vacías no produce lotes", () => {
  assert.deepStrictEqual(trocear([], []), []);
});

test("trocear con un solo lote deja ambas listas juntas", () => {
  const lotes = trocear([A], [EQ]);
  assert.strictEqual(lotes.length, 1);
  assert.deepStrictEqual(lotes[0], { participanteIds: [A], equipoIds: [EQ] });
});
