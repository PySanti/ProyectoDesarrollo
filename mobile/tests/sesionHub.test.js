import test from "node:test";
import assert from "node:assert/strict";
import { sesionHubUrl } from "../src/features/partidas/sesionHub.js";

test("sesionHubUrl arma la URL del hub sin doble slash", () => {
  assert.equal(sesionHubUrl("http://gw:5080"), "http://gw:5080/operaciones-sesion/hubs/sesion");
  assert.equal(sesionHubUrl("http://gw:5080/"), "http://gw:5080/operaciones-sesion/hubs/sesion");
});
