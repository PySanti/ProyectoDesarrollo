import test from "node:test";
import assert from "node:assert/strict";
import { cargarLive, debeRecargarLive, enviarRespuestaTrivia } from "../src/features/partidas/partidaLiveFlow.js";

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

test("debeRecargarLive: fases terminales no se recargan (no se pisa el ranking final)", () => {
  // Bug: al terminar, un refresh de token re-dispara load() y cargarLive devuelve
  // sin-participacion, borrando el consolidado. Una fase terminal no debe recargarse.
  assert.equal(debeRecargarLive("finalizada"), false);
  assert.equal(debeRecargarLive("cancelada"), false);
});

test("debeRecargarLive: fases no terminales sí se recargan", () => {
  assert.equal(debeRecargarLive("iniciada"), true);
  assert.equal(debeRecargarLive("lobby"), true);
  assert.equal(debeRecargarLive("sin-participacion"), true);
  assert.equal(debeRecargarLive("cargando"), true);
});

test("enviarRespuestaTrivia: si la pregunta avanza durante el envio, avanzo=true", async () => {
  // Simula PreguntaActivada llegando por SignalR mientras el POST esta en vuelo:
  // resetSignal (leido via el getter) cambia entre el inicio y el fin del await.
  let resetSignal = 0;
  const responder = async () => {
    resetSignal = 1;
    return { ok: true, data: { esCorrecta: true, cerroPregunta: true, puntaje: 10 } };
  };
  const res = await enviarRespuestaTrivia(responder, () => resetSignal);
  assert.equal(res.avanzo, true);
  assert.equal(res.r.ok, true);
});

test("enviarRespuestaTrivia: sin avance, avanzo=false y pasa la respuesta tal cual", async () => {
  const resetSignal = 3;
  const responder = async () => ({ ok: false, type: "conflict", message: "dup" });
  const res = await enviarRespuestaTrivia(responder, () => resetSignal);
  assert.equal(res.avanzo, false);
  assert.equal(res.r.type, "conflict");
});
