import test from "node:test";
import assert from "node:assert/strict";
import { getRendimientoMiEquipo } from "../src/features/puntuaciones/rendimientoEquipoApi.js";

// HU-49: encadena GET /identity/teams/mine (equipoId del equipo activo) →
// GET /puntuaciones/equipos/{equipoId}/rendimiento. Patrón de historialPartidasApi.test.js.
const token = "x." + Buffer.from(JSON.stringify({ sub: "u1" })).toString("base64url") + ".y";

const jsonResponse = (status, body) => ({
  ok: status >= 200 && status < 300,
  status,
  json: async () => body,
});

test("getRendimientoMiEquipo con equipo activo resuelve equipoId y devuelve el rendimiento parseado", async () => {
  const calls = [];
  const rendimiento = {
    equipoId: "e1",
    partidas: [
      { partidaId: "p1", fechaFin: "2026-07-01T00:00:00Z", posicion: 1, gano: true },
    ],
  };
  const fetchImpl = async (url, init) => {
    calls.push({ url, init });
    if (url.endsWith("/identity/teams/mine")) {
      return jsonResponse(200, { equipoId: "e1", nombreEquipo: "Los Rayos" });
    }
    return jsonResponse(200, rendimiento);
  };

  const r = await getRendimientoMiEquipo("http://gw", token, fetchImpl);

  assert.equal(r.ok, true);
  assert.equal(calls[0].url, "http://gw/identity/teams/mine");
  assert.equal(calls[0].init.headers.Authorization, "Bearer " + token);
  assert.equal(calls[1].url, "http://gw/puntuaciones/equipos/e1/rendimiento");
  assert.equal(calls[1].init.headers.Authorization, "Bearer " + token);
  assert.deepEqual(r.data, rendimiento);
});

test("getRendimientoMiEquipo sin equipo activo (teams/mine 404) devuelve estado sin-equipo", async () => {
  const fetchImpl = async (url) => {
    if (url.endsWith("/identity/teams/mine")) return jsonResponse(404, {});
    throw new Error("no debería llamar a rendimiento");
  };
  const r = await getRendimientoMiEquipo("http://gw", token, fetchImpl);
  assert.equal(r.ok, false);
  assert.equal(r.type, "sinEquipo");
  assert.match(r.message, /No perteneces a un equipo activo/);
});

test("getRendimientoMiEquipo con teams/mine 200 pero sin equipoId devuelve estado sin-equipo", async () => {
  const fetchImpl = async (url) => {
    if (url.endsWith("/identity/teams/mine")) return jsonResponse(200, {});
    throw new Error("no debería llamar a rendimiento");
  };
  const r = await getRendimientoMiEquipo("http://gw", token, fetchImpl);
  assert.equal(r.ok, false);
  assert.equal(r.type, "sinEquipo");
});

test("getRendimientoMiEquipo con error al cargar rendimiento devuelve shape de error", async () => {
  const fetchImpl = async (url) => {
    if (url.endsWith("/identity/teams/mine")) return jsonResponse(200, { equipoId: "e1" });
    return jsonResponse(500, {});
  };
  const r = await getRendimientoMiEquipo("http://gw", token, fetchImpl);
  assert.equal(r.ok, false);
  assert.equal(r.type, "error");
  assert.match(r.message, /No se pudo cargar el rendimiento/);
});

test("getRendimientoMiEquipo devuelve unauthorized si teams/mine responde 401", async () => {
  const fetchImpl = async (url) => {
    if (url.endsWith("/identity/teams/mine")) return jsonResponse(401, {});
    throw new Error("no debería llamar a rendimiento");
  };
  const r = await getRendimientoMiEquipo("http://gw", token, fetchImpl);
  assert.equal(r.ok, false);
  assert.equal(r.type, "unauthorized");
});

test("getRendimientoMiEquipo devuelve unauthorized si rendimiento responde 401", async () => {
  const fetchImpl = async (url) => {
    if (url.endsWith("/identity/teams/mine")) return jsonResponse(200, { equipoId: "e1" });
    return jsonResponse(401, {});
  };
  const r = await getRendimientoMiEquipo("http://gw", token, fetchImpl);
  assert.equal(r.ok, false);
  assert.equal(r.type, "unauthorized");
});

test("getRendimientoMiEquipo devuelve error de red si teams/mine lanza", async () => {
  const fetchImpl = async () => {
    throw new Error("boom");
  };
  const r = await getRendimientoMiEquipo("http://gw", token, fetchImpl);
  assert.equal(r.ok, false);
  assert.equal(r.type, "network");
  assert.match(r.message, /No se pudo conectar/);
});

test("getRendimientoMiEquipo devuelve error de red si rendimiento lanza", async () => {
  const fetchImpl = async (url) => {
    if (url.endsWith("/identity/teams/mine")) return jsonResponse(200, { equipoId: "e1" });
    throw new Error("boom");
  };
  const r = await getRendimientoMiEquipo("http://gw", token, fetchImpl);
  assert.equal(r.ok, false);
  assert.equal(r.type, "network");
});

test("getRendimientoMiEquipo con 200 y body de rendimiento sin partidas devuelve lista vacia", async () => {
  const fetchImpl = async (url) => {
    if (url.endsWith("/identity/teams/mine")) return jsonResponse(200, { equipoId: "e1" });
    return jsonResponse(200, { equipoId: "e1" });
  };
  const r = await getRendimientoMiEquipo("http://gw", token, fetchImpl);
  assert.equal(r.ok, true);
  assert.deepEqual(r.data.partidas, []);
});
