import test from "node:test";
import assert from "node:assert/strict";
import {
  getPreguntaActual,
  responderPregunta,
  getRankingJuego,
  getRankingConsolidado,
  getEtapaActual,
  validarTesoro,
} from "../src/features/partidas/gameplayApi.js";

const jsonResponse = (status, body) => ({
  ok: status >= 200 && status < 300,
  status,
  json: async () => body,
});

test("getPreguntaActual 200 devuelve pregunta y 409 devuelve sin_pregunta", async () => {
  const okImpl = async (url, init) => {
    assert.equal(url, "http://gw/operaciones-sesion/partidas/p1/pregunta-actual");
    assert.equal(init.headers.Authorization, "Bearer tok");
    return jsonResponse(200, { preguntaId: "q1", texto: "2+2?", opciones: [] });
  };
  const r1 = await getPreguntaActual("http://gw", "tok", "p1", okImpl);
  assert.equal(r1.ok, true);
  assert.equal(r1.pregunta.preguntaId, "q1");

  const sinImpl = async () => jsonResponse(409, { message: "sin pregunta activa" });
  const r2 = await getPreguntaActual("http://gw", "tok", "p1", sinImpl);
  assert.equal(r2.ok, false);
  assert.equal(r2.type, "sin_pregunta");
});

test("responderPregunta POST body opcionId y 409 duplicada → conflict", async () => {
  const calls = [];
  const okImpl = async (url, init) => {
    calls.push({ url, method: init.method, body: init.body });
    return jsonResponse(200, { esCorrecta: true, cerroPregunta: true, puntaje: 10 });
  };
  const r1 = await responderPregunta("http://gw", "tok", "p1", "op1", okImpl);
  assert.equal(r1.ok, true);
  assert.equal(r1.data.puntaje, 10);
  assert.deepEqual(calls, [{
    url: "http://gw/operaciones-sesion/partidas/p1/pregunta-actual/respuesta",
    method: "POST",
    body: JSON.stringify({ opcionId: "op1" }),
  }]);

  const dupImpl = async () => jsonResponse(409, { message: "Ya respondiste esta pregunta." });
  const r2 = await responderPregunta("http://gw", "tok", "p1", "op1", dupImpl);
  assert.equal(r2.ok, false);
  assert.equal(r2.type, "conflict");
  assert.equal(r2.message, "Ya respondiste esta pregunta.");
});

test("getRankingJuego y getRankingConsolidado arman URLs de puntuaciones", async () => {
  const urls = [];
  const impl = async (url) => {
    urls.push(url);
    return jsonResponse(200, { entradas: [] });
  };
  const r1 = await getRankingJuego("http://gw", "tok", "p1", "j1", impl);
  assert.equal(r1.ok, true);
  const r2 = await getRankingConsolidado("http://gw", "tok", "p1", impl);
  assert.equal(r2.ok, true);
  assert.deepEqual(urls, [
    "http://gw/puntuaciones/partidas/p1/juegos/j1/ranking",
    "http://gw/puntuaciones/partidas/p1/ranking-consolidado",
  ]);
});

test("consolidado 409 (no terminada) mapea conflict", async () => {
  const impl = async () => jsonResponse(409, { message: "no terminada" });
  const r = await getRankingConsolidado("http://gw", "tok", "p1", impl);
  assert.equal(r.ok, false);
  assert.equal(r.type, "conflict");
});

test("getEtapaActual 200 devuelve etapa y 409 devuelve sin_etapa", async () => {
  const okImpl = async (url, init) => {
    assert.equal(url, "http://gw/operaciones-sesion/partidas/p1/etapa-actual");
    assert.equal(init.headers.Authorization, "Bearer tok");
    return jsonResponse(200, { etapaId: "e1", orden: 1, areaBusqueda: "Plaza central" });
  };
  const r1 = await getEtapaActual("http://gw", "tok", "p1", okImpl);
  assert.equal(r1.ok, true);
  assert.equal(r1.etapa.etapaId, "e1");

  const sinImpl = async () => jsonResponse(409, { message: "sin etapa activa" });
  const r2 = await getEtapaActual("http://gw", "tok", "p1", sinImpl);
  assert.equal(r2.ok, false);
  assert.equal(r2.type, "sin_etapa");
});

test("validarTesoro POST body imagenBase64; Invalido es 200 ok:true", async () => {
  const calls = [];
  const impl = async (url, init) => {
    calls.push({ url, method: init.method, body: init.body });
    return jsonResponse(200, { resultado: "Invalido", gano: false, cerroEtapa: false, puntaje: null });
  };
  const r = await validarTesoro("http://gw", "tok", "p1", "QkFTRTY0", impl);
  assert.equal(r.ok, true);
  assert.equal(r.data.resultado, "Invalido");
  assert.deepEqual(calls, [{
    url: "http://gw/operaciones-sesion/partidas/p1/etapa-actual/tesoro",
    method: "POST",
    body: JSON.stringify({ imagenBase64: "QkFTRTY0" }),
  }]);

  const errImpl = async () => jsonResponse(403, { message: "No inscrito." });
  const r2 = await validarTesoro("http://gw", "tok", "p1", "QkFTRTY0", errImpl);
  assert.equal(r2.ok, false);
});
