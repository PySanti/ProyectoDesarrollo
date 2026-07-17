import test from "node:test";
import assert from "node:assert/strict";

import { getPartidasPublicadas } from "../src/features/partidas/partidasPublicadasApi.js";
import {
  inscribirse,
  cancelarInscripcion,
  preinscribirEquipo,
} from "../src/features/partidas/inscripcionApi.js";
import { getMisConvocatorias, aceptarConvocatoria } from "../src/features/partidas/convocatoriasApi.js";
import { getMiSesion } from "../src/features/partidas/miSesionApi.js";

const jsonResponse = (status, body) => ({
  ok: status >= 200 && status < 300,
  status,
  json: async () => body,
});

test("getPartidasPublicadas hace GET autenticado y devuelve data", async () => {
  const calls = [];
  const fetchImpl = async (url, init) => {
    calls.push({ url, init });
    return jsonResponse(200, [{ partidaId: "p1", nombre: "Copa", modalidad: "Individual" }]);
  };
  const r = await getPartidasPublicadas("http://gw", "tok", fetchImpl);
  assert.equal(r.ok, true);
  assert.equal(r.data[0].nombre, "Copa");
  assert.equal(calls[0].url, "http://gw/operaciones-sesion/partidas-publicadas");
  assert.equal(calls[0].init.headers.Authorization, "Bearer tok");
});

test("getPartidasPublicadas mapea fallo de red", async () => {
  const fetchImpl = async () => {
    throw new Error("boom");
  };
  const r = await getPartidasPublicadas("http://gw", "tok", fetchImpl);
  assert.equal(r.ok, false);
  assert.equal(r.type, "network");
});

test("inscribirse POST correcto y mapea 409 a conflict con mensaje del backend", async () => {
  const okImpl = async (url, init) => {
    assert.equal(url, "http://gw/operaciones-sesion/partidas/p1/inscripciones");
    assert.equal(init.method, "POST");
    return jsonResponse(201, { inscripcionId: "i1" });
  };
  const r1 = await inscribirse("http://gw", "tok", "p1", okImpl);
  assert.equal(r1.ok, true);
  assert.equal(r1.data.inscripcionId, "i1");

  const conflictImpl = async () => jsonResponse(409, { message: "Ya tienes una participacion activa." });
  const r2 = await inscribirse("http://gw", "tok", "p1", conflictImpl);
  assert.equal(r2.ok, false);
  assert.equal(r2.type, "conflict");
  assert.equal(r2.message, "Ya tienes una participacion activa.");
});

test("cancelarInscripcion DELETE a inscripciones/mia", async () => {
  const calls = [];
  const fetchImpl = async (url, init) => {
    calls.push({ url, init });
    return { ok: true, status: 204, json: async () => ({}) };
  };
  const r = await cancelarInscripcion("http://gw", "tok", "p1", fetchImpl);
  assert.equal(r.ok, true);
  assert.equal(calls[0].url, "http://gw/operaciones-sesion/partidas/p1/inscripciones/mia");
  assert.equal(calls[0].init.method, "DELETE");
});

test("preinscribirEquipo mapea 403 no-lider", async () => {
  const fetchImpl = async () => jsonResponse(403, { message: "Solo el lider puede preinscribir." });
  const r = await preinscribirEquipo("http://gw", "tok", "p1", fetchImpl);
  assert.equal(r.ok, false);
  assert.equal(r.type, "forbidden");
});

test("getMisConvocatorias GET y aceptarConvocatoria POST", async () => {
  const listImpl = async (url) => {
    assert.equal(url, "http://gw/operaciones-sesion/mis-convocatorias");
    return jsonResponse(200, [{ convocatoriaId: "c1", partidaId: "p1", equipoId: "e1" }]);
  };
  const r1 = await getMisConvocatorias("http://gw", "tok", listImpl);
  assert.equal(r1.ok, true);
  assert.equal(r1.data.length, 1);

  const acceptImpl = async (url, init) => {
    assert.equal(url, "http://gw/operaciones-sesion/convocatorias/c1/aceptacion");
    assert.equal(init.method, "POST");
    return jsonResponse(200, { estado: "Aceptada" });
  };
  const r2 = await aceptarConvocatoria("http://gw", "tok", "c1", acceptImpl);
  assert.equal(r2.ok, true);
});

test("getMiSesion 200 devuelve sesion y 204 devuelve null", async () => {
  const conSesion = async () => jsonResponse(200, { partidaId: "p1", estadoPartida: "Lobby" });
  const r1 = await getMiSesion("http://gw", "tok", conSesion);
  assert.equal(r1.ok, true);
  assert.equal(r1.sesion.partidaId, "p1");

  const sinSesion = async () => ({ ok: true, status: 204, json: async () => ({}) });
  const r2 = await getMiSesion("http://gw", "tok", sinSesion);
  assert.equal(r2.ok, true);
  assert.equal(r2.sesion, null);
});
