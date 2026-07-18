import test from "node:test";
import assert from "node:assert/strict";
import {
  getPreguntaActual,
  responderPregunta,
  getRankingJuego,
  getRankingConsolidado,
  getEtapaActual,
  validarTesoro,
  formatRespuestaCorrecta,
  seleccionarRespuestaCorrecta,
  aplicaRespuestaEquipo,
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

test("formatRespuestaCorrecta arma el aviso de cierre o null si no hay texto (HU-24)", () => {
  assert.equal(formatRespuestaCorrecta("Madrid"), "La respuesta correcta era: Madrid");
  assert.equal(formatRespuestaCorrecta(null), null);
  assert.equal(formatRespuestaCorrecta(undefined), null);
  assert.equal(formatRespuestaCorrecta(""), null);
});

test("seleccionarRespuestaCorrecta filtra por juegoId (anti-leak al cambiar de juego, 7d review)", () => {
  // Mismo juego → pasa el texto.
  assert.equal(
    seleccionarRespuestaCorrecta({ texto: "Madrid", juegoId: "j1" }, "j1"),
    "Madrid",
  );
  // PreguntaCerrada de un juego anterior sigue en memoria pero el juego activo cambió: no debe filtrar.
  assert.equal(seleccionarRespuestaCorrecta({ texto: "Madrid", juegoId: "j1" }, "j2"), null);
  // Sin cierre todavía.
  assert.equal(seleccionarRespuestaCorrecta(null, "j1"), null);
  // Cierre del juego actual sin texto (payload aditivo, backend no lo mandó).
  assert.equal(seleccionarRespuestaCorrecta({ texto: null, juegoId: "j1" }, "j1"), null);
});

test("aplicaRespuestaEquipo: acepta el evento de la pregunta que se esta mostrando", () => {
  // En Equipo, la respuesta de un companero sella al equipo: su resultado debe pintarse igual
  // en el resto de los telefonos (RespuestaEquipoRegistrada).
  assert.equal(
    aplicaRespuestaEquipo({ juegoId: "j1", preguntaId: "q1", esCorrecta: false }, "j1", "q1"),
    true,
  );
});

test("aplicaRespuestaEquipo: descarta eventos tardios o de otro juego/pregunta", () => {
  // Un evento que llega tarde (ya avanzo la pregunta) no debe sellar la pregunta nueva.
  const ev = { juegoId: "j1", preguntaId: "q1", esCorrecta: false };
  assert.equal(aplicaRespuestaEquipo(ev, "j1", "q2"), false);
  assert.equal(aplicaRespuestaEquipo(ev, "j2", "q1"), false);
  assert.equal(aplicaRespuestaEquipo(null, "j1", "q1"), false);
  assert.equal(aplicaRespuestaEquipo(ev, "j1", null), false);
});

test("aplicaRespuestaEquipo: NO sella en acierto (un acierto cierra y avanza para todos)", () => {
  // El bug: sellar tambien en acierto dejaba al equipo ganador clavado en "Correcto", porque el
  // sello competia con el avance de pregunta. Un acierto no debe sellar; el avance normal
  // (PreguntaActivada) lleva a la pregunta nueva. Solo el fallo sella (bloquea al equipo).
  assert.equal(aplicaRespuestaEquipo({ juegoId: "j1", preguntaId: "q1", esCorrecta: true }, "j1", "q1"), false);
});
