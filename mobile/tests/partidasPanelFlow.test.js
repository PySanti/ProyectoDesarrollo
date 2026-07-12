import test from "node:test";
import assert from "node:assert/strict";
import { cargarPanel, filtrarPorModalidad } from "../src/features/partidas/partidasPanelFlow.js";

const jsonResponse = (status, body) => ({
  ok: status >= 200 && status < 300,
  status,
  json: async () => body,
});

test("cargarPanel combina listado + mi-sesion", async () => {
  const fetchImpl = async (url) => {
    if (url.endsWith("/partidas-publicadas")) {
      return jsonResponse(200, [{ partidaId: "p1", nombre: "Copa", modalidad: "Individual" }]);
    }
    if (url.endsWith("/mi-sesion")) {
      return jsonResponse(200, { partidaId: "p9", estadoPartida: "Lobby" });
    }
    throw new Error(`URL inesperada: ${url}`);
  };
  const r = await cargarPanel({ apiBaseUrl: "http://gw", token: "tok", fetchImpl });
  assert.equal(r.ok, true);
  assert.equal(r.partidas.length, 1);
  assert.equal(r.miSesion.partidaId, "p9");
});

test("cargarPanel con listado caido reporta error pero mi-sesion 204 no bloquea", async () => {
  const fetchImpl = async (url) => {
    if (url.endsWith("/partidas-publicadas")) {
      return jsonResponse(500, { message: "boom" });
    }
    return { ok: true, status: 204, json: async () => ({}) };
  };
  const r = await cargarPanel({ apiBaseUrl: "http://gw", token: "tok", fetchImpl });
  assert.equal(r.ok, false);
});

test("filtrarPorModalidad", () => {
  const partidas = [
    { partidaId: "a", modalidad: "Individual" },
    { partidaId: "b", modalidad: "Equipo" },
  ];
  assert.equal(filtrarPorModalidad(partidas, "Todas").length, 2);
  assert.deepEqual(filtrarPorModalidad(partidas, "Equipo").map((p) => p.partidaId), ["b"]);
});
