import { beforeEach, describe, expect, it, vi } from "vitest";

describe("bdtApi", () => {
  beforeEach(() => {
    vi.resetModules();
    vi.unstubAllEnvs();
  });

  it("calls HU-37 operator published games endpoint with bearer token", async () => {
    vi.stubEnv("VITE_BDT_API_BASE_URL", "https://bdt.example.test/");
    const { getOperatorPublishedBdtGames } = await import("./bdtApi");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => [
        {
          partidaId: "partida-1",
          nombre: "Busqueda QR Campus",
          modalidad: "Individual",
          estado: "Lobby",
          areaBusqueda: "Patio central",
          cantidadEtapas: 2
        }
      ]
    });

    const result = await getOperatorPublishedBdtGames("operator-token", fetchMock as unknown as typeof fetch);

    expect(fetchMock).toHaveBeenCalledWith("https://bdt.example.test/api/bdt/operator/games/published", {
      method: "GET",
      headers: {
        Authorization: "Bearer operator-token"
      }
    });
    expect(result).toHaveLength(1);
    expect(result[0].nombre).toBe("Busqueda QR Campus");
  });

  it("maps HU-37 non-OK responses to BdtApiError", async () => {
    vi.stubEnv("VITE_BDT_API_BASE_URL", "https://bdt.example.test");
    const { BdtApiError, getOperatorPublishedBdtGames } = await import("./bdtApi");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: false,
      status: 403,
      json: async () => ({ message: "forbidden" })
    });

    await expect(getOperatorPublishedBdtGames("participant-token", fetchMock as unknown as typeof fetch))
      .rejects
      .toMatchObject({ name: "BdtApiError", message: "forbidden", statusCode: 403 });

    await expect(getOperatorPublishedBdtGames("participant-token", fetchMock as unknown as typeof fetch))
      .rejects
      .toBeInstanceOf(BdtApiError);
  });

  it("calls HU-43 start endpoint with bearer token", async () => {
    vi.stubEnv("VITE_BDT_API_BASE_URL", "https://bdt.example.test/");
    const { startBdtGame } = await import("./bdtApi");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({
        partidaId: "partida-1",
        nombre: "Busqueda QR Campus",
        estado: "Iniciada",
        modalidad: "Individual",
        etapaActiva: {
          etapaId: "etapa-1",
          orden: 1,
          tiempoLimiteSegundos: 300,
          iniciadaEnUtc: "2026-01-01T00:00:00Z",
          cierraEnUtc: "2026-01-01T00:05:00Z"
        },
        mensaje: "Partida BDT iniciada."
      })
    });

    const result = await startBdtGame("partida-1", "operator-token", fetchMock as unknown as typeof fetch);

    expect(fetchMock).toHaveBeenCalledWith("https://bdt.example.test/api/bdt/games/partida-1/start", {
      method: "POST",
      headers: {
        Authorization: "Bearer operator-token"
      }
    });
    expect(result.estado).toBe("Iniciada");
    expect(result.etapaActiva.orden).toBe(1);
  });

  it("maps HU-43 start non-OK responses to BdtApiError", async () => {
    vi.stubEnv("VITE_BDT_API_BASE_URL", "https://bdt.example.test");
    const { BdtApiError, startBdtGame } = await import("./bdtApi");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: false,
      status: 409,
      json: async () => ({ message: "La BDT no cumple el minimo de participacion configurado." })
    });

    await expect(startBdtGame("partida-1", "operator-token", fetchMock as unknown as typeof fetch))
      .rejects
      .toMatchObject({
        name: "BdtApiError",
        message: "La BDT no cumple el minimo de participacion configurado.",
        statusCode: 409
      });

    await expect(startBdtGame("partida-1", "operator-token", fetchMock as unknown as typeof fetch))
      .rejects
      .toBeInstanceOf(BdtApiError);
  });

  it("calls HU-34 expected QR decode endpoint with multipart image and bearer token", async () => {
    vi.stubEnv("VITE_BDT_API_BASE_URL", "https://bdt.example.test/");
    const { decodeBdtExpectedQrImage } = await import("./bdtApi");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({
        estadoProcesamiento: "Decodificado",
        qrDecodificado: "QR-ETAPA-1",
        mensaje: "QR decodificado correctamente."
      })
    });

    const result = await decodeBdtExpectedQrImage(
      new File(["QR:QR-ETAPA-1"], "qr.png", { type: "image/png" }),
      "operator-token",
      fetchMock as unknown as typeof fetch
    );

    expect(fetchMock).toHaveBeenCalledWith("https://bdt.example.test/api/bdt/stages/expected-qr/decode", {
      method: "POST",
      headers: {
        Authorization: "Bearer operator-token"
      },
      body: expect.any(FormData)
    });
    expect(result.qrDecodificado).toBe("QR-ETAPA-1");
  });
});
