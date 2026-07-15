import test from "node:test";
import assert from "node:assert/strict";
import { reengancharAlReconectar } from "../src/features/partidas/sesionHub.js";

function fakeHub() {
  return {
    invocaciones: [],
    handlers: [],
    onreconnected(cb) { this.handlers.push(cb); },
    invoke(metodo, arg) { this.invocaciones.push([metodo, arg]); return Promise.resolve(); },
  };
}

test("al reconectar re-invoca SuscribirAPartida", async () => {
  const hub = fakeHub();
  reengancharAlReconectar(hub, "p1");

  await hub.handlers[0]();

  assert.deepEqual(hub.invocaciones, [["SuscribirAPartida", "p1"]]);
});

test("un fallo al re-suscribirse no propaga", async () => {
  const hub = fakeHub();
  hub.invoke = () => Promise.reject(new Error("caido"));
  reengancharAlReconectar(hub, "p1");

  // El handler es fire-and-forget (devuelve undefined), asi que no hay promesa que esperar.
  // Sin el .catch del impl, node reportaria una unhandled rejection y este test fallaria:
  // la pantalla debe seguir operable con Recargar en vez de romperse.
  assert.doesNotThrow(() => hub.handlers[0]());
  await new Promise((r) => setImmediate(r));
});
