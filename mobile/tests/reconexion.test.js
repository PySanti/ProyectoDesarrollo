import test from "node:test";
import assert from "node:assert/strict";
import { reconexionIndefinida } from "../src/features/partidas/reconexion.js";

const delay = (previousRetryCount) =>
  reconexionIndefinida.nextRetryDelayInMilliseconds({ previousRetryCount });

test("backoff corto al principio", () => {
  assert.equal(delay(0), 0);
  assert.equal(delay(1), 2000);
  assert.equal(delay(2), 5000);
  assert.equal(delay(3), 10000);
});

test("luego reintenta cada 30s", () => {
  assert.equal(delay(4), 30000);
  assert.equal(delay(10), 30000);
});

test("nunca se rinde: siempre devuelve un numero, jamas null", () => {
  for (const n of [0, 1, 5, 50, 500, 9999]) {
    const d = delay(n);
    assert.equal(typeof d, "number", `previousRetryCount=${n} debe dar numero`);
    assert.ok(d >= 0, `previousRetryCount=${n} debe ser >= 0`);
  }
});
