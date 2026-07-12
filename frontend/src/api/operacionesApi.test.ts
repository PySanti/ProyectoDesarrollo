import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  avanzarEtapa,
  avanzarPregunta,
  enviarPista,
  finalizarJuegoActual,
  getEtapaActual,
  getEstadoSesion,
  getLobby,
  getPreguntaActual,
  iniciarPartida,
  OperacionesApiError,
  publicarPartida
} from "./operacionesApi";

const okJson = (body: unknown, status = 200) =>
  vi.fn().mockResolvedValue(new Response(JSON.stringify(body), { status }));

describe("operacionesApi", () => {
  beforeEach(() => vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test/"));
  afterEach(() => vi.unstubAllEnvs());

  it("publicarPartida hace POST a publicacion con bearer y devuelve LobbyDto", async () => {
    const fetchImpl = okJson({ partidaId: "p1", estado: "Lobby", inscritosActivos: 0 }, 201);
    const r = await publicarPartida("p1", "tok", fetchImpl);
    expect(r.estado).toBe("Lobby");
    expect(fetchImpl.mock.calls[0][0]).toBe(
      "https://gw.example.test/operaciones-sesion/partidas/p1/publicacion"
    );
    expect(fetchImpl.mock.calls[0][1].method).toBe("POST");
    expect((fetchImpl.mock.calls[0][1].headers as Record<string, string>).Authorization).toBe(
      "Bearer tok"
    );
  });

  it("getLobby hace GET al lobby", async () => {
    const fetchImpl = okJson({ partidaId: "p1", estado: "Lobby", inscritosActivos: 2 });
    await getLobby("p1", "tok", fetchImpl);
    expect(fetchImpl.mock.calls[0][0]).toBe("https://gw.example.test/operaciones-sesion/partidas/p1/lobby");
    expect(fetchImpl.mock.calls[0][1].method).toBe("GET");
  });

  it("iniciarPartida hace POST a inicio y acepta estado Cancelada (200) como resultado", async () => {
    const fetchImpl = okJson({ partidaId: "p1", estado: "Cancelada" });
    const r = await iniciarPartida("p1", "tok", fetchImpl);
    expect(r.estado).toBe("Cancelada");
    expect(fetchImpl.mock.calls[0][0]).toBe("https://gw.example.test/operaciones-sesion/partidas/p1/inicio");
    expect(fetchImpl.mock.calls[0][1].method).toBe("POST");
  });

  it("getEstadoSesion hace GET al estado", async () => {
    const fetchImpl = okJson({ partidaId: "p1", estado: "Iniciada", modalidad: "Individual", juegos: [] });
    const r = await getEstadoSesion("p1", "tok", fetchImpl);
    expect(r.estado).toBe("Iniciada");
    expect(fetchImpl.mock.calls[0][0]).toBe("https://gw.example.test/operaciones-sesion/partidas/p1/estado");
  });

  it("error del backend lanza OperacionesApiError con status y message", async () => {
    const f409 = okJson({ message: "ya publicada" }, 409);
    await expect(publicarPartida("p1", "tok", f409)).rejects.toMatchObject({
      statusCode: 409,
      message: "ya publicada"
    });
    const f404 = okJson({ message: "no publicada" }, 404);
    await expect(getLobby("p1", "tok", f404)).rejects.toBeInstanceOf(OperacionesApiError);
  });

  it("getPreguntaActual hace GET a pregunta-actual y un 409 lanza error con statusCode", async () => {
    const ok = okJson({
      partidaId: "p1",
      juegoId: "j1",
      preguntaId: "q1",
      orden: 1,
      texto: "2+2?",
      tiempoLimiteSegundos: 30,
      fechaActivacion: "2026-07-08T12:00:00Z",
      opciones: [{ opcionId: "o1", texto: "4" }]
    });
    const r = await getPreguntaActual("p1", "tok", ok);
    expect(r.opciones[0].opcionId).toBe("o1");
    expect(ok.mock.calls[0][0]).toBe(
      "https://gw.example.test/operaciones-sesion/partidas/p1/pregunta-actual"
    );
    expect(ok.mock.calls[0][1].method).toBe("GET");

    const sin = okJson({ message: "sin pregunta activa" }, 409);
    await expect(getPreguntaActual("p1", "tok", sin)).rejects.toMatchObject({ statusCode: 409 });
  });

  it("avanzarPregunta hace POST al avance y devuelve sinMasPreguntas", async () => {
    const f = okJson({ partidaId: "p1", preguntaCerradaOrden: 2, preguntaActivadaOrden: null, sinMasPreguntas: true });
    const r = await avanzarPregunta("p1", "tok", f);
    expect(r.sinMasPreguntas).toBe(true);
    expect(f.mock.calls[0][0]).toBe(
      "https://gw.example.test/operaciones-sesion/partidas/p1/pregunta-actual/avance"
    );
    expect(f.mock.calls[0][1].method).toBe("POST");
  });

  it("finalizarJuegoActual hace POST a la finalizacion y devuelve terminada", async () => {
    const f = okJson({ partidaId: "p1", estado: "Terminada", juegoFinalizadoOrden: 1, juegoActivadoOrden: null, terminada: true });
    const r = await finalizarJuegoActual("p1", "tok", f);
    expect(r.terminada).toBe(true);
    expect(f.mock.calls[0][0]).toBe(
      "https://gw.example.test/operaciones-sesion/partidas/p1/juego-actual/finalizacion"
    );
    expect(f.mock.calls[0][1].method).toBe("POST");
  });

  it("getEtapaActual hace GET a etapa-actual; 409 lanza error con statusCode", async () => {
    const ok = okJson({
      partidaId: "p1", juegoId: "j1", etapaId: "e1", orden: 1,
      areaBusqueda: "Plaza central", tiempoLimiteSegundos: 120, fechaActivacion: "2026-07-08T12:00:00Z"
    });
    const r = await getEtapaActual("p1", "tok", ok);
    expect(r.areaBusqueda).toBe("Plaza central");
    expect(ok.mock.calls[0][0]).toBe("https://gw.example.test/operaciones-sesion/partidas/p1/etapa-actual");
    expect(ok.mock.calls[0][1].method).toBe("GET");
    const sin = okJson({ message: "sin etapa activa" }, 409);
    await expect(getEtapaActual("p1", "tok", sin)).rejects.toMatchObject({ statusCode: 409 });
  });

  it("avanzarEtapa hace POST al avance y devuelve sinMasEtapas", async () => {
    const f = okJson({ partidaId: "p1", etapaCerradaOrden: 2, etapaActivadaOrden: null, sinMasEtapas: true });
    const r = await avanzarEtapa("p1", "tok", f);
    expect(r.sinMasEtapas).toBe(true);
    expect(f.mock.calls[0][0]).toBe("https://gw.example.test/operaciones-sesion/partidas/p1/etapa-actual/avance");
    expect(f.mock.calls[0][1].method).toBe("POST");
  });

  it("enviarPista hace POST a pistas con el cuerpo y devuelve timestamp", async () => {
    const f = okJson({ partidaId: "p1", juegoId: "j1", participanteDestinoId: "u1", equipoDestinoId: null, timestampUtc: "2026-07-08T12:00:00Z" });
    const r = await enviarPista("p1", { texto: "busca cerca del arbol", participanteDestinoId: "u1" }, "tok", f);
    expect(r.participanteDestinoId).toBe("u1");
    expect(f.mock.calls[0][0]).toBe("https://gw.example.test/operaciones-sesion/partidas/p1/pistas");
    expect(f.mock.calls[0][1].method).toBe("POST");
    expect(JSON.parse(f.mock.calls[0][1].body as string)).toMatchObject({ texto: "busca cerca del arbol", participanteDestinoId: "u1" });
  });
});
