import test from "node:test";
import assert from "node:assert/strict";
import {
  cargarNombresPartida,
  nombreCortoPartida,
  nombrePartidaEnCache,
  nombrePartidaResuelto,
  resetNombresPartidaCache,
  trocearPartidas,
} from "../src/features/shared/useNombresPartida.js";

const P1 = "aaaaaaaa-0000-0000-0000-000000000000";
const P2 = "bbbbbbbb-0000-0000-0000-000000000000";

const okFetch = (partidas) => async () => ({
  ok: true,
  status: 200,
  json: async () => ({ partidas }),
});

test("nombreCortoPartida recorta el GUID a 8 caracteres", () => {
  assert.strictEqual(nombreCortoPartida(P1), "aaaaaaaa");
});

test("trocearPartidas reparte en lotes de 200", () => {
  const muchos = Array.from({ length: 250 }, (_, i) => `${String(i).padStart(8, "0")}-x`);
  const lotes = trocearPartidas(muchos);
  assert.strictEqual(lotes.length, 2);
  assert.strictEqual(lotes[0].length, 200);
  assert.strictEqual(lotes[1].length, 50);
});

test("trocearPartidas con lista vacia no produce lotes", () => {
  assert.deepStrictEqual(trocearPartidas([]), []);
});

test("cargarNombresPartida resuelve y deja el nombre en cache", async () => {
  resetNombresPartidaCache();
  const fetchImpl = okFetch([{ partidaId: P1, nombre: "Copa UMBRAL" }]);

  await cargarNombresPartida([P1], "http://gw", "tok", fetchImpl);

  assert.strictEqual(nombrePartidaEnCache(P1), "Copa UMBRAL");
});

test("cargarNombresPartida no repide lo que ya esta en cache", async () => {
  resetNombresPartidaCache();
  let llamadas = 0;
  const fetchImpl = async () => {
    llamadas++;
    return { ok: true, status: 200, json: async () => ({ partidas: [{ partidaId: P1, nombre: "Copa" }] }) };
  };

  await cargarNombresPartida([P1], "http://gw", "tok", fetchImpl);
  await cargarNombresPartida([P1], "http://gw", "tok", fetchImpl);

  assert.strictEqual(llamadas, 1);
});

test("cargarNombresPartida cachea como no-resoluble lo pedido que no vuelve", async () => {
  resetNombresPartidaCache();
  let llamadas = 0;
  const fetchImpl = async () => {
    llamadas++;
    return { ok: true, status: 200, json: async () => ({ partidas: [] }) };
  };

  await cargarNombresPartida([P2], "http://gw", "tok", fetchImpl);
  await cargarNombresPartida([P2], "http://gw", "tok", fetchImpl);

  // Sin esto entraria en bucle de repeticion: el id nunca se resolveria y siempre faltaria.
  assert.strictEqual(llamadas, 1);
  assert.strictEqual(nombrePartidaEnCache(P2), "bbbbbbbb");
});

test("cargarNombresPartida degrada al GUID corto si el directorio falla", async () => {
  resetNombresPartidaCache();
  const fetchImpl = async () => {
    throw new Error("boom");
  };

  await cargarNombresPartida([P1], "http://gw", "tok", fetchImpl);

  // Resolver un nombre nunca rompe la pantalla: cae al GUID y sigue operativa.
  assert.strictEqual(nombrePartidaEnCache(P1), "aaaaaaaa");
});

test("nombrePartidaResuelto devuelve null sin resolver y el nombre tras resolver", async () => {
  resetNombresPartidaCache();

  // Sin fallback a GUID: quien lo llama elige su propio texto por defecto.
  assert.strictEqual(nombrePartidaResuelto(P1), null);

  await cargarNombresPartida([P1], "http://gw", "tok", okFetch([{ partidaId: P1, nombre: "Copa UMBRAL" }]));

  assert.strictEqual(nombrePartidaResuelto(P1), "Copa UMBRAL");
});

test("nombrePartidaResuelto sigue en null si se pidio y no vino", async () => {
  resetNombresPartidaCache();

  await cargarNombresPartida([P2], "http://gw", "tok", okFetch([]));

  // La cache guarda null para no repedirlo; el llamador no debe distinguirlo de "nunca pedido".
  assert.strictEqual(nombrePartidaResuelto(P2), null);
});

test("cargarNombresPartida con fallo no envenena la cache: un reintento posterior resuelve", async () => {
  resetNombresPartidaCache();
  const fallo = async () => {
    throw new Error("boom");
  };

  await cargarNombresPartida([P1], "http://gw", "tok", fallo);
  await cargarNombresPartida([P1], "http://gw", "tok", okFetch([{ partidaId: P1, nombre: "Copa UMBRAL" }]));

  assert.strictEqual(nombrePartidaEnCache(P1), "Copa UMBRAL");
});
