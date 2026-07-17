import test from "node:test";
import assert from "node:assert/strict";
import { resolverNombresPartida } from "../src/features/shared/partidaDirectoryApi.js";

const jsonResponse = (status, body) => ({
  ok: status >= 200 && status < 300,
  status,
  json: async () => body,
});

test("resolverNombresPartida hace POST autenticado al directorio de Operaciones", async () => {
  const calls = [];
  const fetchImpl = async (url, init) => {
    calls.push({ url, init });
    return jsonResponse(200, { partidas: [{ partidaId: "p1", nombre: "Copa UMBRAL" }] });
  };

  const r = await resolverNombresPartida("http://gw", "tok", { partidaIds: ["p1"] }, fetchImpl);

  assert.equal(r.ok, true);
  // Operaciones, no Partidas: el gateway le cierra /partidas/** al Participante.
  assert.equal(calls[0].url, "http://gw/operaciones-sesion/directory/partidas");
  assert.equal(calls[0].init.method, "POST");
  assert.equal(calls[0].init.headers.Authorization, "Bearer tok");
  assert.equal(calls[0].init.headers["Content-Type"], "application/json");
  assert.deepEqual(JSON.parse(calls[0].init.body), { partidaIds: ["p1"] });
  assert.deepEqual(r.data.partidas, [{ partidaId: "p1", nombre: "Copa UMBRAL" }]);
});

test("resolverNombresPartida con 200 y body sin partidas devuelve lista vacia", async () => {
  const fetchImpl = async () => jsonResponse(200, {});
  const r = await resolverNombresPartida("http://gw", "tok", { partidaIds: [] }, fetchImpl);
  assert.equal(r.ok, true);
  assert.deepEqual(r.data.partidas, []);
});

test("resolverNombresPartida devuelve unauthorized en 401", async () => {
  const fetchImpl = async () => jsonResponse(401, {});
  const r = await resolverNombresPartida("http://gw", "tok", { partidaIds: ["p1"] }, fetchImpl);
  assert.equal(r.ok, false);
  assert.equal(r.type, "unauthorized");
});

test("resolverNombresPartida devuelve error generico en 400 del tope", async () => {
  const fetchImpl = async () => jsonResponse(400, { message: "El lote no puede superar 200 ids." });
  const r = await resolverNombresPartida("http://gw", "tok", { partidaIds: ["p1"] }, fetchImpl);
  assert.equal(r.ok, false);
  assert.equal(r.type, "error");
  assert.match(r.message, /200 ids/);
});

test("resolverNombresPartida devuelve error de red si fetch lanza", async () => {
  const fetchImpl = async () => {
    throw new Error("boom");
  };
  const r = await resolverNombresPartida("http://gw", "tok", { partidaIds: ["p1"] }, fetchImpl);
  assert.equal(r.ok, false);
  assert.equal(r.type, "network");
});
