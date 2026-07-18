import test from "node:test";
import assert from "node:assert/strict";
import { fetchConvocatorias, responderConvocatoria, destinoTrasResponder } from "../src/features/partidas/convocatoriasFlow.js";

const jsonResponse = (status, body) => ({
  ok: status >= 200 && status < 300,
  status,
  json: async () => body,
});

test("fetchConvocatorias devuelve data", async () => {
  const fetchImpl = async () => jsonResponse(200, [{ convocatoriaId: "c1", partidaId: "p1", equipoId: "e1" }]);
  const r = await fetchConvocatorias({ apiBaseUrl: "http://gw", token: "tok", fetchImpl });
  assert.equal(r.ok, true);
  assert.equal(r.data[0].convocatoriaId, "c1");
});

// Red de seguridad: el movil pinta este nombre y el gateway le cierra /partidas/**,
// asi que un mapeo futuro que lo pierda lo dejaria sin forma de nombrar la partida.
test("fetchConvocatorias propaga nombrePartida del backend", async () => {
  const fetchImpl = async () =>
    jsonResponse(200, [
      {
        convocatoriaId: "c1",
        partidaId: "p1",
        equipoId: "e1",
        fechaEnvio: "2026-07-08T12:00:00Z",
        nombrePartida: "Copa UCAB",
      },
    ]);
  const r = await fetchConvocatorias({ apiBaseUrl: "http://gw", token: "tok", fetchImpl });
  assert.equal(r.ok, true);
  assert.equal(r.data[0].nombrePartida, "Copa UCAB");
});

// Al aceptar, el miembro debe ir al lobby de la partida (mismo lugar donde el lider
// espera el inicio): sin esto no se suscribe al grupo y su panel no cambia al iniciar.
test("destinoTrasResponder: aceptar lleva al lobby con partidaId y nombre", () => {
  const conv = { convocatoriaId: "c1", partidaId: "p1", nombrePartida: "Copa UCAB" };
  assert.deepEqual(destinoTrasResponder(conv, true), { partidaId: "p1", nombre: "Copa UCAB" });
});

test("destinoTrasResponder: rechazar no navega", () => {
  const conv = { convocatoriaId: "c1", partidaId: "p1", nombrePartida: "Copa UCAB" };
  assert.equal(destinoTrasResponder(conv, false), null);
});

test("responderConvocatoria aceptar=false hace POST rechazo y mapea 409", async () => {
  const calls = [];
  const fetchImpl = async (url, init) => {
    calls.push({ url, method: init.method });
    return jsonResponse(409, { message: "La partida ya no esta en lobby." });
  };
  const r = await responderConvocatoria({
    apiBaseUrl: "http://gw", token: "tok", convocatoriaId: "c1", aceptar: false, fetchImpl,
  });
  assert.equal(r.ok, false);
  assert.equal(r.type, "conflict");
  assert.deepEqual(calls, [{ url: "http://gw/operaciones-sesion/convocatorias/c1/rechazo", method: "POST" }]);
});
