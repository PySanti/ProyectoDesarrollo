import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  addJuegoBdt,
  addJuegoTrivia,
  createPartida,
  getPartida,
  getPartidas,
  PartidasApiError
} from "./partidasApi";

const okJson = (body: unknown, status = 200) =>
  vi.fn().mockResolvedValue(new Response(JSON.stringify(body), { status }));

describe("partidasApi", () => {
  beforeEach(() => {
    vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test/");
  });
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("createPartida hace POST /partidas con bearer y devuelve partidaId", async () => {
    const fetchImpl = okJson({ partidaId: "p-1" }, 201);
    const result = await createPartida(
      {
        nombrePartida: "Copa",
        modalidad: "Individual",
        modoInicioPartida: "Manual",
        tiempoInicio: null,
        minimosParticipacion: 1,
        maximosParticipacion: 10
      },
      "tok",
      fetchImpl
    );
    expect(result.partidaId).toBe("p-1");
    const [url, init] = fetchImpl.mock.calls[0];
    expect(url).toBe("https://gw.example.test/partidas");
    expect(init.method).toBe("POST");
    expect((init.headers as Record<string, string>).Authorization).toBe("Bearer tok");
  });

  it("addJuegoTrivia y addJuegoBdt pegan a la subruta correcta", async () => {
    const fetchImpl = okJson({ juegoId: "j-1" }, 201);
    await addJuegoTrivia("p-1", { orden: 1, preguntas: [] }, "tok", fetchImpl);
    expect(fetchImpl.mock.calls[0][0]).toBe("https://gw.example.test/partidas/p-1/juegos/trivia");
    await addJuegoBdt("p-1", { orden: 2, areaBusqueda: "x", etapas: [] }, "tok", fetchImpl);
    expect(fetchImpl.mock.calls[1][0]).toBe("https://gw.example.test/partidas/p-1/juegos/bdt");
  });

  it("getPartidas y getPartida hacen GET autenticado", async () => {
    const fetchImpl = okJson([]);
    await getPartidas("tok", fetchImpl);
    expect(fetchImpl.mock.calls[0][0]).toBe("https://gw.example.test/partidas");
    const fetchOne = okJson({ partidaId: "p-1", juegos: [] });
    await getPartida("p-1", "tok", fetchOne);
    expect(fetchOne.mock.calls[0][0]).toBe("https://gw.example.test/partidas/p-1");
  });

  it("error del backend lanza PartidasApiError con status y message", async () => {
    const fetchImpl = okJson({ message: "orden duplicado" }, 409);
    await expect(
      addJuegoTrivia("p-1", { orden: 1, preguntas: [] }, "tok", fetchImpl)
    ).rejects.toMatchObject({ statusCode: 409, message: "orden duplicado" });
    await expect(
      addJuegoTrivia("p-1", { orden: 1, preguntas: [] }, "tok", fetchImpl)
    ).rejects.toBeInstanceOf(PartidasApiError);
  });

  it("sin VITE_GATEWAY_BASE_URL lanza error claro", async () => {
    vi.unstubAllEnvs();
    vi.stubEnv("VITE_GATEWAY_BASE_URL", "");
    await expect(getPartidas("tok", okJson([]))).rejects.toThrow(
      "Missing VITE_GATEWAY_BASE_URL"
    );
  });
});
