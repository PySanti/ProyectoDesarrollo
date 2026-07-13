import test from "node:test";
import assert from "node:assert/strict";
import { getHistorialPartidas } from "../src/features/puntuaciones/historialPartidasApi.js";

// token con sub "u1" (header.payload.sig, payload en base64url), mismo patrón que
// partidaLobbyFlow.test.js.
const token = "x." + Buffer.from(JSON.stringify({ sub: "u1" })).toString("base64url") + ".y";

const jsonResponse = (status, body) => ({
  ok: status >= 200 && status < 300,
  status,
  json: async () => body,
});

test("getHistorialPartidas hace GET autenticado con el sub del token y devuelve la estructura parseada", async () => {
  const calls = [];
  const payload = {
    participanteId: "u1",
    partidas: [
      {
        partidaId: "p1",
        modalidad: "Individual",
        fechaFin: "2026-07-01T00:00:00Z",
        equipoId: null,
        puntosTotales: 45,
        posicion: 1,
        gano: true,
        juegos: [{ juegoId: "j1", orden: 1, tipoJuego: "Trivia", puntos: 20 }],
      },
    ],
  };
  const fetchImpl = async (url, init) => {
    calls.push({ url, init });
    return jsonResponse(200, payload);
  };

  const r = await getHistorialPartidas("http://gw", token, fetchImpl);

  assert.equal(r.ok, true);
  assert.equal(calls[0].url, "http://gw/puntuaciones/participantes/u1/historial-partidas");
  assert.equal(calls[0].init.headers.Authorization, "Bearer " + token);
  assert.deepEqual(r.data, payload);
});

test("getHistorialPartidas con 200 y body sin partidas devuelve lista vacia", async () => {
  const fetchImpl = async () => jsonResponse(200, { participanteId: "u1" });
  const r = await getHistorialPartidas("http://gw", token, fetchImpl);
  assert.equal(r.ok, true);
  assert.deepEqual(r.data.partidas, []);
});

test("getHistorialPartidas con 204 (sin body) devuelve lista vacia", async () => {
  const fetchImpl = async () => ({
    ok: true,
    status: 204,
    json: async () => {
      throw new Error("no body");
    },
  });
  const r = await getHistorialPartidas("http://gw", token, fetchImpl);
  assert.equal(r.ok, true);
  assert.deepEqual(r.data.partidas, []);
});

test("getHistorialPartidas devuelve unauthorized en 401", async () => {
  const fetchImpl = async () => jsonResponse(401, {});
  const r = await getHistorialPartidas("http://gw", token, fetchImpl);
  assert.equal(r.ok, false);
  assert.equal(r.type, "unauthorized");
  assert.match(r.message, /Sesión expirada/);
});

test("getHistorialPartidas devuelve error generico en otros status no-ok", async () => {
  const fetchImpl = async () => jsonResponse(500, {});
  const r = await getHistorialPartidas("http://gw", token, fetchImpl);
  assert.equal(r.ok, false);
  assert.equal(r.type, "error");
  assert.match(r.message, /No se pudo cargar/);
});

test("getHistorialPartidas devuelve error de red si fetch lanza", async () => {
  const fetchImpl = async () => {
    throw new Error("boom");
  };
  const r = await getHistorialPartidas("http://gw", token, fetchImpl);
  assert.equal(r.ok, false);
  assert.equal(r.type, "network");
  assert.match(r.message, /No se pudo conectar/);
});
