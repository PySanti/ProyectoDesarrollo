import test from "node:test";
import assert from "node:assert/strict";
import { fetchConvocatorias, responderConvocatoria } from "../src/features/partidas/convocatoriasFlow.js";

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
