import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  aceptarInscripcion,
  avanzarEtapa,
  avanzarPregunta,
  cancelarPartida,
  enviarPista,
  finalizarJuegoActual,
  getEnviosTesoro,
  getEtapaActual,
  getEstadoSesion,
  getLobby,
  getPreguntaActual,
  iniciarPartida,
  OperacionesApiError,
  publicarPartida,
  rechazarInscripcion
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

  it("aceptarInscripcion hace POST a la ruta de aceptación y devuelve el LobbyDto", async () => {
    const lobby = {
      partidaId: "p1",
      sesionPartidaId: "s1",
      estado: "Lobby",
      modalidad: "Individual",
      minimosParticipacion: 1,
      maximosParticipacion: 10,
      inscritosActivos: 1,
      participantes: ["u1"],
      equipos: [],
      solicitudesPendientesIndividual: [],
      solicitudesPendientesEquipo: []
    };
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify(lobby), { status: 200 })
    );

    const result = await aceptarInscripcion("p1", "i1", "token-abc", fetchMock);

    expect(fetchMock).toHaveBeenCalledWith(
      "https://gw.example.test/operaciones-sesion/partidas/p1/inscripciones/i1/aceptacion",
      expect.objectContaining({ method: "POST" })
    );
    expect(result.inscritosActivos).toBe(1);
  });

  it("rechazarInscripcion hace POST a la ruta de rechazo", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ solicitudesPendientesIndividual: [], solicitudesPendientesEquipo: [] }), { status: 200 })
    );

    await rechazarInscripcion("p1", "i1", "token-abc", fetchMock);

    expect(fetchMock).toHaveBeenCalledWith(
      "https://gw.example.test/operaciones-sesion/partidas/p1/inscripciones/i1/rechazo",
      expect.objectContaining({ method: "POST" })
    );
  });

  it("aceptarInscripcion propaga el error del backend (409 solicitud no pendiente)", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ message: "La inscripción no está pendiente." }), { status: 409 })
    );

    await expect(aceptarInscripcion("p1", "i1", "t", fetchMock)).rejects.toMatchObject({
      name: "OperacionesApiError",
      statusCode: 409
    });
  });

  it("cancelarPartida hace POST a la ruta de cancelacion y devuelve el estado", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ partidaId: "p1", estado: "Cancelada" }), { status: 200 })
    );

    const result = await cancelarPartida("p1", "token-abc", fetchMock);

    expect(fetchMock).toHaveBeenCalledWith(
      "https://gw.example.test/operaciones-sesion/partidas/p1/cancelacion",
      expect.objectContaining({ method: "POST" })
    );
    expect((fetchMock.mock.calls[0][1].headers as Record<string, string>).Authorization).toBe(
      "Bearer token-abc"
    );
    expect(result).toEqual({ partidaId: "p1", estado: "Cancelada" });
  });

  it("cancelarPartida propaga el error del backend (409 estado terminal)", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ message: "La partida ya está en un estado terminal." }), { status: 409 })
    );

    await expect(cancelarPartida("p1", "t", fetchMock)).rejects.toMatchObject({
      name: "OperacionesApiError",
      statusCode: 409
    });
  });

  it("getEnviosTesoro hace GET a juego-actual/envios-tesoro y devuelve etapas con intentos", async () => {
    const dto = {
      partidaId: "p1",
      juegoId: "j1",
      etapas: [
        {
          etapaId: "e1",
          orden: 1,
          intentos: [
            { participanteId: "u1", equipoId: null, resultado: "Invalido", instante: "2026-07-12T10:00:00Z" },
            { participanteId: "u2", equipoId: null, resultado: "Valido", instante: "2026-07-12T10:01:00Z" }
          ]
        }
      ]
    };
    const fetchImpl = okJson(dto);
    const r = await getEnviosTesoro("p1", "tok", fetchImpl);
    expect(r.etapas[0].intentos).toHaveLength(2);
    expect(fetchImpl.mock.calls[0][0]).toBe(
      "https://gw.example.test/operaciones-sesion/partidas/p1/juego-actual/envios-tesoro"
    );
    expect(fetchImpl.mock.calls[0][1].method).toBe("GET");
  });

  it("getEnviosTesoro propaga 409 cuando el juego activo no es BDT", async () => {
    const fetchImpl = okJson({ message: "juego activo no es BDT" }, 409);
    await expect(getEnviosTesoro("p1", "tok", fetchImpl)).rejects.toMatchObject({
      name: "OperacionesApiError",
      statusCode: 409
    });
  });

  it("cancelarPartida propaga 404 cuando la sesion no existe", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ message: "no publicada" }), { status: 404 })
    );

    await expect(cancelarPartida("p1", "t", fetchMock)).rejects.toMatchObject({
      name: "OperacionesApiError",
      statusCode: 404
    });
  });
});
