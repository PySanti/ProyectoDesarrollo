import test from "node:test";
import assert from "node:assert/strict";
import { correo, nombreEquipo, nombrePersona } from "../src/shared/validation.js";

test("nombreEquipo rejects symbols-only and empty, accepts a real name", () => {
  for (const value of ["****", "1234", "   ", ""]) {
    assert.notEqual(nombreEquipo(value), null, `deberia rechazar ${JSON.stringify(value)}`);
  }
  assert.equal(nombreEquipo("Los Gordos"), null);
});

test("nombrePersona rejects symbols-only, accepts accents", () => {
  assert.notEqual(nombrePersona("****"), null);
  assert.equal(nombrePersona("José Pérez"), null);
});

test("correo rejects malformed, accepts valid", () => {
  for (const value of ["", "sin-arroba", "a@b", "@b.com"]) {
    assert.notEqual(correo(value), null, `deberia rechazar ${JSON.stringify(value)}`);
  }
  assert.equal(correo("ana@test.com"), null);
});
