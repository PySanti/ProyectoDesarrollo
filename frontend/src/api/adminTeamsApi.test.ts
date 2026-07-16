import { beforeEach, describe, expect, it, vi } from "vitest";

describe("adminTeamsApi", () => {
  beforeEach(() => {
    vi.resetModules();
    vi.unstubAllEnvs();
  });

  it("GET lista de equipos con bearer token", async () => {
    vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test/");
    const { listAdminTeams } = await import("./adminTeamsApi");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => [
        {
          equipoId: "e1",
          nombreEquipo: "Los Ganadores",
          estado: "Activo",
          liderUserId: "u1",
          integrantes: [{ usuarioId: "u1", esLider: true }]
        }
      ]
    });

    const result = await listAdminTeams("admin-token", fetchMock as unknown as typeof fetch);

    expect(fetchMock).toHaveBeenCalledWith("https://gw.example.test/identity/admin/teams", {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
        Authorization: "Bearer admin-token"
      }
    });
    expect(result).toHaveLength(1);
    expect(result[0].equipoId).toBe("e1");
  });

  it("GET equipo por id 404 lanza IdentityApiError con statusCode 404", async () => {
    vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test");
    const { IdentityApiError, getAdminTeam } = await import("./adminTeamsApi");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: false,
      status: 404,
      json: async () => ({ message: "Equipo no encontrado" })
    });

    await expect(
      getAdminTeam("e404", "admin-token", fetchMock as unknown as typeof fetch)
    ).rejects.toMatchObject({ name: "IdentityApiError", message: "Equipo no encontrado", statusCode: 404 });

    await expect(
      getAdminTeam("e404", "admin-token", fetchMock as unknown as typeof fetch)
    ).rejects.toBeInstanceOf(IdentityApiError);

    expect(fetchMock).toHaveBeenCalledWith("https://gw.example.test/identity/admin/teams/e404", {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
        Authorization: "Bearer admin-token"
      }
    });
  });

  it("POST crea equipo con { nombreEquipo, liderUserId }", async () => {
    vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test");
    const { createAdminTeam } = await import("./adminTeamsApi");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 201,
      json: async () => ({
        equipoId: "e2",
        nombreEquipo: "Nuevo Equipo",
        estado: "Activo",
        liderUserId: "u9",
        integrantes: [{ usuarioId: "u9", esLider: true }]
      })
    });

    const result = await createAdminTeam(
      { nombreEquipo: "Nuevo Equipo", liderUserId: "u9" },
      "admin-token",
      fetchMock as unknown as typeof fetch
    );

    expect(fetchMock).toHaveBeenCalledWith("https://gw.example.test/identity/admin/teams", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: "Bearer admin-token"
      },
      body: JSON.stringify({ nombreEquipo: "Nuevo Equipo", liderUserId: "u9" })
    });
    expect(result.equipoId).toBe("e2");
  });

  it("PATCH renombra equipo con { nombreEquipo }", async () => {
    vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test");
    const { renameAdminTeam } = await import("./adminTeamsApi");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({
        equipoId: "e1",
        nombreEquipo: "Renombrado",
        estado: "Activo",
        liderUserId: "u1",
        integrantes: [{ usuarioId: "u1", esLider: true }]
      })
    });

    const result = await renameAdminTeam(
      "e1",
      { nombreEquipo: "Renombrado" },
      "admin-token",
      fetchMock as unknown as typeof fetch
    );

    expect(fetchMock).toHaveBeenCalledWith("https://gw.example.test/identity/admin/teams/e1/name", {
      method: "PATCH",
      headers: {
        "Content-Type": "application/json",
        Authorization: "Bearer admin-token"
      },
      body: JSON.stringify({ nombreEquipo: "Renombrado" })
    });
    expect(result.nombreEquipo).toBe("Renombrado");
  });

  it("PATCH reasigna liderazgo con { nuevoLiderUserId }", async () => {
    vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test");
    const { reassignAdminTeamLeader } = await import("./adminTeamsApi");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({
        equipoId: "e1",
        nombreEquipo: "Los Ganadores",
        estado: "Activo",
        liderUserId: "u2",
        integrantes: [
          { usuarioId: "u1", esLider: false },
          { usuarioId: "u2", esLider: true }
        ]
      })
    });

    const result = await reassignAdminTeamLeader(
      "e1",
      { nuevoLiderUserId: "u2" },
      "admin-token",
      fetchMock as unknown as typeof fetch
    );

    expect(fetchMock).toHaveBeenCalledWith(
      "https://gw.example.test/identity/admin/teams/e1/leadership",
      {
        method: "PATCH",
        headers: {
          "Content-Type": "application/json",
          Authorization: "Bearer admin-token"
        },
        body: JSON.stringify({ nuevoLiderUserId: "u2" })
      }
    );
    expect(result.liderUserId).toBe("u2");
  });

  it("PATCH cambia estado con { estado }", async () => {
    vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test");
    const { setAdminTeamEstado } = await import("./adminTeamsApi");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({
        equipoId: "e1",
        nombreEquipo: "Los Ganadores",
        estado: "Desactivado",
        liderUserId: "u1",
        integrantes: [{ usuarioId: "u1", esLider: true }]
      })
    });

    const result = await setAdminTeamEstado(
      "e1",
      { estado: "Desactivado" },
      "admin-token",
      fetchMock as unknown as typeof fetch
    );

    expect(fetchMock).toHaveBeenCalledWith(
      "https://gw.example.test/identity/admin/teams/e1/estado",
      {
        method: "PATCH",
        headers: {
          "Content-Type": "application/json",
          Authorization: "Bearer admin-token"
        },
        body: JSON.stringify({ estado: "Desactivado" })
      }
    );
    expect(result.estado).toBe("Desactivado");
  });

  it("DELETE 200 resuelve con el equipo eliminado", async () => {
    vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test");
    const { deleteAdminTeam } = await import("./adminTeamsApi");
    const outcome = {
      equipoId: "e1",
      nombreEquipo: "Los Halcones"
    };
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => outcome
    });

    await expect(
      deleteAdminTeam("e1", "admin-token", fetchMock as unknown as typeof fetch)
    ).resolves.toEqual(outcome);

    expect(fetchMock).toHaveBeenCalledWith("https://gw.example.test/identity/admin/teams/e1", {
      method: "DELETE",
      headers: {
        "Content-Type": "application/json",
        Authorization: "Bearer admin-token"
      }
    });
  });

  it("DELETE 409 lanza IdentityApiError con statusCode 409 (participacion activa)", async () => {
    vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test");
    const { IdentityApiError, deleteAdminTeam } = await import("./adminTeamsApi");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: false,
      status: 409,
      json: async () => ({ message: "El equipo tiene una participacion activa" })
    });

    await expect(
      deleteAdminTeam("e1", "admin-token", fetchMock as unknown as typeof fetch)
    ).rejects.toMatchObject({
      name: "IdentityApiError",
      statusCode: 409,
      message: "El equipo tiene una participacion activa"
    });

    await expect(
      deleteAdminTeam("e1", "admin-token", fetchMock as unknown as typeof fetch)
    ).rejects.toBeInstanceOf(IdentityApiError);
  });

  it("falla claramente cuando falta VITE_GATEWAY_BASE_URL", async () => {
    vi.stubEnv("VITE_GATEWAY_BASE_URL", "");
    const { listAdminTeams } = await import("./adminTeamsApi");

    await expect(listAdminTeams("admin-token", vi.fn() as unknown as typeof fetch)).rejects.toThrow(
      "Missing VITE_GATEWAY_BASE_URL environment variable."
    );
  });
});
