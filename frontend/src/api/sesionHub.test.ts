import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

describe("sesionHub", () => {
  beforeEach(() => vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test/"));
  afterEach(() => {
    vi.unstubAllEnvs();
    vi.resetModules();
    vi.restoreAllMocks();
  });

  it("sesionHubUrl arma el prefijo operaciones-sesion del hub", async () => {
    const { sesionHubUrl } = await import("./sesionHub");
    expect(sesionHubUrl()).toBe("https://gw.example.test/operaciones-sesion/hubs/sesion");
  });

  it("crearSesionHub configura withUrl con la url del hub y accessTokenFactory que devuelve el token", async () => {
    const build = vi.fn(() => ({ __fake: true }));
    const withAutomaticReconnect = vi.fn(() => ({ build }));
    const withUrl = vi.fn(() => ({ withAutomaticReconnect }));
    const HubConnectionBuilder = vi.fn(() => ({ withUrl }));
    vi.doMock("@microsoft/signalr", () => ({ HubConnectionBuilder }));

    const { crearSesionHub, sesionHubUrl } = await import("./sesionHub");
    const conn = crearSesionHub(() => "tok");

    expect(conn).toEqual({ __fake: true });
    expect(withUrl).toHaveBeenCalledTimes(1);
    const [url, options] = withUrl.mock.calls[0] as unknown as [
      string,
      { accessTokenFactory: () => string }
    ];
    expect(url).toBe(sesionHubUrl());
    expect(options.accessTokenFactory()).toBe("tok");
    expect(withAutomaticReconnect).toHaveBeenCalled();
  });
});
