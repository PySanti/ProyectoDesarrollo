import { beforeEach, describe, expect, it, vi } from "vitest";

const SUB = "abcdef12-0000-0000-0000-000000000000";

function fakeFetch(status: number, body: unknown) {
  return vi.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body)
  } as unknown as Response);
}

// directoryApi lee VITE_GATEWAY_BASE_URL al cargarse, así que hay que stubear el env
// antes de importarlo (mismo patrón que adminTeamsApi.test.ts).
describe("resolverNombres", () => {
  beforeEach(() => {
    vi.resetModules();
    vi.unstubAllEnvs();
  });

  it("hace POST al gateway con el cuerpo y el token", async () => {
    vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test");
    const { resolverNombres } = await import("./directoryApi");
    const fetchImpl = fakeFetch(200, { participantes: [{ participanteId: SUB, nombre: "María González" }], equipos: [] });

    const result = await resolverNombres({ participanteIds: [SUB], equipoIds: [] }, "tok", fetchImpl);

    expect(result.participantes[0].nombre).toBe("María González");
    const [url, init] = fetchImpl.mock.calls[0];
    expect(url).toBe("https://gw.example.test/identity/directory/names");
    expect(init.method).toBe("POST");
    expect(JSON.parse(init.body)).toEqual({ participanteIds: [SUB], equipoIds: [] });
    expect(init.headers.Authorization).toBe("Bearer tok");
  });

  it("lanza IdentityApiError con el status cuando la respuesta no es ok", async () => {
    vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test");
    const { resolverNombres } = await import("./directoryApi");
    const fetchImpl = fakeFetch(400, { message: "lote demasiado grande" });

    await expect(resolverNombres({ participanteIds: [], equipoIds: [] }, "tok", fetchImpl))
      .rejects.toMatchObject({ statusCode: 400 });
  });

  it("normaliza a listas vacías si el cuerpo viene sin ellas", async () => {
    vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test");
    const { resolverNombres } = await import("./directoryApi");
    const fetchImpl = fakeFetch(200, {});

    const result = await resolverNombres({ participanteIds: [], equipoIds: [] }, "tok", fetchImpl);

    expect(result).toEqual({ participantes: [], equipos: [] });
  });
});
