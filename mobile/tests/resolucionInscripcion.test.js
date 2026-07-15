import test from "node:test";
import assert from "node:assert/strict";
import { avisoResolucion } from "../src/features/partidas/resolucionInscripcion.js";

test("aceptada da aviso de exito", () => {
  const r = avisoResolucion(true);
  assert.equal(r.variant, "success");
  assert.match(r.texto, /confirmada|dentro/i);
});

test("rechazada dice que se puede volver a solicitar", () => {
  // El backend permite re-solicitar (OcupaParticipacion solo cuenta Pendiente|Activa),
  // asi que el copy no debe sugerir que sea terminal.
  const r = avisoResolucion(false);
  assert.equal(r.variant, "error");
  assert.match(r.texto, /volver a solicitar/i);
});
