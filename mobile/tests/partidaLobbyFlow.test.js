import test from "node:test";
import assert from "node:assert/strict";
import { cargarLobby, accionParticipacion, avisoLiderEquipo } from "../src/features/partidas/partidaLobbyFlow.js";

const jsonResponse = (status, body) => ({
  ok: status >= 200 && status < 300,
  status,
  json: async () => body,
});

test("cargarLobby trae lobby y marca inscrito si mi-sesion apunta a la partida", async () => {
  const fetchImpl = async (url) => {
    if (url.endsWith("/lobby")) {
      return jsonResponse(200, { partidaId: "p1", estado: "Lobby", modalidad: "Individual", inscritosActivos: 2 });
    }
    if (url.endsWith("/mi-sesion")) {
      return jsonResponse(200, { partidaId: "p1", estadoPartida: "Lobby" });
    }
    throw new Error(`URL inesperada: ${url}`);
  };
  const r = await cargarLobby({ apiBaseUrl: "http://gw", token: "tok", partidaId: "p1", fetchImpl });
  assert.equal(r.ok, true);
  assert.equal(r.lobby.inscritosActivos, 2);
  assert.equal(r.inscrito, true);
});

test("cargarLobby con mi-sesion en otra partida marca inscrito false", async () => {
  const fetchImpl = async (url) => {
    if (url.endsWith("/lobby")) {
      return jsonResponse(200, { partidaId: "p1", estado: "Lobby", modalidad: "Individual", inscritosActivos: 0 });
    }
    return jsonResponse(200, { partidaId: "OTRA", estadoPartida: "Lobby" });
  };
  const r = await cargarLobby({ apiBaseUrl: "http://gw", token: "tok", partidaId: "p1", fetchImpl });
  assert.equal(r.inscrito, false);
});

test("cargarLobby expone estadoInscripcion Pendiente desde mi-sesion.inscripcion.estado", async () => {
  const fetchImpl = async (url) => {
    if (url.endsWith("/lobby")) {
      return jsonResponse(200, { partidaId: "p1", estado: "Lobby", modalidad: "Individual", inscritosActivos: 1 });
    }
    if (url.endsWith("/mi-sesion")) {
      return jsonResponse(200, {
        partidaId: "p1",
        inscripcion: { inscripcionId: "i1", estado: "Pendiente" },
      });
    }
    throw new Error(`URL inesperada: ${url}`);
  };
  const r = await cargarLobby({ apiBaseUrl: "http://gw", token: "tok", partidaId: "p1", fetchImpl });
  assert.equal(r.estadoInscripcion, "Pendiente");
  assert.equal(r.inscrito, true);
});

test("cargarLobby sin sesion activa deja estadoInscripcion en null", async () => {
  const fetchImpl = async (url) => {
    if (url.endsWith("/lobby")) {
      return jsonResponse(200, { partidaId: "p1", estado: "Lobby", modalidad: "Individual", inscritosActivos: 0 });
    }
    return { ok: true, status: 204, json: async () => ({}) };
  };
  const r = await cargarLobby({ apiBaseUrl: "http://gw", token: "tok", partidaId: "p1", fetchImpl });
  assert.equal(r.estadoInscripcion, null);
});

test("accionParticipacion Individual no inscrito hace POST inscripciones", async () => {
  const calls = [];
  const fetchImpl = async (url, init) => {
    calls.push({ url, method: init.method });
    return jsonResponse(201, { inscripcionId: "i1" });
  };
  const r = await accionParticipacion({
    apiBaseUrl: "http://gw", token: "tok", partidaId: "p1",
    modalidad: "Individual", inscrito: false, fetchImpl,
  });
  assert.equal(r.ok, true);
  assert.deepEqual(calls, [{ url: "http://gw/operaciones-sesion/partidas/p1/inscripciones", method: "POST" }]);
});

test("accionParticipacion Equipo inscrito hace DELETE inscripciones-equipo/mia", async () => {
  const calls = [];
  const fetchImpl = async (url, init) => {
    calls.push({ url, method: init.method });
    return { ok: true, status: 204, json: async () => ({}) };
  };
  const r = await accionParticipacion({
    apiBaseUrl: "http://gw", token: "tok", partidaId: "p1",
    modalidad: "Equipo", inscrito: true, fetchImpl,
  });
  assert.equal(r.ok, true);
  assert.deepEqual(calls, [{ url: "http://gw/operaciones-sesion/partidas/p1/inscripciones-equipo/mia", method: "DELETE" }]);
});

test("cargarLobby en Equipo marca esLider segun teams/mine", async () => {
  // token con payload {"sub":"lider-1"} en base64url (header.payload.sig)
  const token = "x." + Buffer.from(JSON.stringify({ sub: "lider-1" })).toString("base64url") + ".y";
  // Forma real del contrato (contracts/http/identity-api.md): participantes[].{usuarioId, esLider}
  const make = (esLiderMio) => async (url) => {
    if (url.endsWith("/lobby")) {
      return jsonResponse(200, { partidaId: "p1", estado: "Lobby", modalidad: "Equipo", inscritosActivos: 0 });
    }
    if (url.endsWith("/mi-sesion")) {
      return { ok: true, status: 204, json: async () => ({}) };
    }
    if (url.endsWith("/identity/teams/mine")) {
      return jsonResponse(200, {
        equipoId: "e1",
        nombreEquipo: "Equipo 1",
        estado: "Activo",
        participantes: [
          { usuarioId: "lider-1", esLider: esLiderMio },
          { usuarioId: "otro", esLider: !esLiderMio },
        ],
      });
    }
    throw new Error(`URL inesperada: ${url}`);
  };
  const lider = await cargarLobby({ apiBaseUrl: "http://gw", token, partidaId: "p1", fetchImpl: make(true) });
  assert.equal(lider.esLider, true);
  const miembro = await cargarLobby({ apiBaseUrl: "http://gw", token, partidaId: "p1", fetchImpl: make(false) });
  assert.equal(miembro.esLider, false);
});

test("cargarLobby Individual no consulta teams/mine y esLider es true", async () => {
  const token = "x." + Buffer.from(JSON.stringify({ sub: "u1" })).toString("base64url") + ".y";
  const urls = [];
  const fetchImpl = async (url) => {
    urls.push(url);
    if (url.endsWith("/lobby")) {
      return jsonResponse(200, { partidaId: "p1", estado: "Lobby", modalidad: "Individual", inscritosActivos: 0 });
    }
    return { ok: true, status: 204, json: async () => ({}) };
  };
  const r = await cargarLobby({ apiBaseUrl: "http://gw", token, partidaId: "p1", fetchImpl });
  assert.equal(r.esLider, true);
  assert.equal(urls.some((u) => u.includes("teams/mine")), false);
});

test("avisoLiderEquipo: Equipo, no lider, sin participacion -> aviso explicito (HU-12)", () => {
  assert.equal(
    avisoLiderEquipo("Equipo", false, false),
    "Solo el líder del equipo puede preinscribir al equipo.",
  );
});

test("avisoLiderEquipo: Equipo, no lider, con participacion -> el lider gestiona", () => {
  assert.equal(
    avisoLiderEquipo("Equipo", false, true),
    "El líder gestiona la preinscripción del equipo.",
  );
});

test("avisoLiderEquipo: lider o Individual -> sin aviso (null, se muestra el boton)", () => {
  assert.equal(avisoLiderEquipo("Equipo", true, false), null);
  assert.equal(avisoLiderEquipo("Individual", false, false), null);
});
