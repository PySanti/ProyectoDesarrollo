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
});
