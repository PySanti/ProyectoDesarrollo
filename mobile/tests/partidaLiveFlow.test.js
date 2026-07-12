import test from "node:test";
import assert from "node:assert/strict";
import { cargarLive } from "../src/features/partidas/partidaLiveFlow.js";

const jsonResponse = (status, body) => ({
  ok: status >= 200 && status < 300,
  status,
  json: async () => body,
});

test("cargarLive con sesion iniciada devuelve fase iniciada + juegoActivo + yaRespondio", async () => {
  const fetchImpl = async () =>
    jsonResponse(200, {
      partidaId: "p1",
      estadoPartida: "Iniciada",
      juegoActivo: { juegoId: "j1", orden: 1, tipoJuego: "Trivia", estadoJuego: "Activo" },
      yaRespondioPreguntaActual: true,
    });
  const r = await cargarLive({ apiBaseUrl: "http://gw", token: "tok", partidaId: "p1", fetchImpl });
  assert.equal(r.ok, true);
  assert.equal(r.fase, "iniciada");
  assert.equal(r.juegoActivo.tipoJuego, "Trivia");
  assert.equal(r.yaRespondio, true);
});

test("cargarLive sin sesion o con otra partida devuelve sin-participacion", async () => {
  const sin = async () => ({ ok: true, status: 204, json: async () => ({}) });
  const r1 = await cargarLive({ apiBaseUrl: "http://gw", token: "tok", partidaId: "p1", fetchImpl: sin });
  assert.equal(r1.fase, "sin-participacion");

  const otra = async () => jsonResponse(200, { partidaId: "OTRA", estadoPartida: "Iniciada" });
  const r2 = await cargarLive({ apiBaseUrl: "http://gw", token: "tok", partidaId: "p1", fetchImpl: otra });
  assert.equal(r2.fase, "sin-participacion");
});

test("cargarLive con sesion en lobby devuelve fase lobby", async () => {
  const fetchImpl = async () => jsonResponse(200, { partidaId: "p1", estadoPartida: "Lobby" });
  const r = await cargarLive({ apiBaseUrl: "http://gw", token: "tok", partidaId: "p1", fetchImpl });
  assert.equal(r.fase, "lobby");
});
