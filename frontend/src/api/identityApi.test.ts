import { beforeEach, describe, expect, it, vi } from "vitest";

describe("identityApi governance", () => {
  beforeEach(() => {
    vi.resetModules();
    vi.unstubAllEnvs();
  });

  it("GET governance roles con bearer token", async () => {
    vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test/");
    const { getGovernanceRoles } = await import("./identityApi");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({
        roles: [
          { rol: "Administrador", permisos: [], privilegiosGobernanza: true },
          { rol: "Operador", permisos: ["GestionarPartidas"], privilegiosGobernanza: false },
          {
            rol: "Participante",
            permisos: ["GestionarEquipos"],
            privilegiosGobernanza: false
          }
        ]
      })
    });

    const result = await getGovernanceRoles("admin-token", fetchMock as unknown as typeof fetch);

    expect(fetchMock).toHaveBeenCalledWith("https://gw.example.test/identity/governance/roles", {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
        Authorization: "Bearer admin-token"
      }
    });
    expect(result.roles).toHaveLength(3);
    expect(result.roles[0].privilegiosGobernanza).toBe(true);
  });

  it("PUT permisos de un rol con set completo en el body", async () => {
    vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test");
    const { updateRolePermissions } = await import("./identityApi");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ rol: "Operador", permisos: ["GestionarEquipos"], privilegiosGobernanza: false })
    });

    const result = await updateRolePermissions(
      "Operador",
      ["GestionarEquipos"],
      "admin-token",
      fetchMock as unknown as typeof fetch
    );

    expect(fetchMock).toHaveBeenCalledWith(
      "https://gw.example.test/identity/governance/roles/Operador/permisos",
      {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
          Authorization: "Bearer admin-token"
        },
        body: JSON.stringify({ permisos: ["GestionarEquipos"] })
      }
    );
    expect(result.permisos).toEqual(["GestionarEquipos"]);
  });

  it("PUT no-ok mapea a IdentityApiError con statusCode", async () => {
    vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test");
    const { IdentityApiError, updateRolePermissions } = await import("./identityApi");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: false,
      status: 502,
      json: async () => ({ message: "keycloak caido" })
    });

    await expect(
      updateRolePermissions("Operador", [], "admin-token", fetchMock as unknown as typeof fetch)
    ).rejects.toMatchObject({ name: "IdentityApiError", message: "keycloak caido", statusCode: 502 });

    await expect(
      updateRolePermissions("Operador", [], "admin-token", fetchMock as unknown as typeof fetch)
    ).rejects.toBeInstanceOf(IdentityApiError);
  });

  it("PATCH cambio de rol con body { rol }", async () => {
    vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test");
    const { changeUserRole } = await import("./identityApi");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ usuarioId: "u1", rol: "Operador" })
    });

    const result = await changeUserRole("u1", "Operador", "admin-token", fetchMock as unknown as typeof fetch);

    expect(fetchMock).toHaveBeenCalledWith("https://gw.example.test/identity/users/u1/role", {
      method: "PATCH",
      headers: {
        "Content-Type": "application/json",
        Authorization: "Bearer admin-token"
      },
      body: JSON.stringify({ rol: "Operador" })
    });
    expect(result.rol).toBe("Operador");
  });

  it("PATCH 409 conserva el mensaje del backend", async () => {
    vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test");
    const { changeUserRole } = await import("./identityApi");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: false,
      status: 409,
      json: async () => ({ message: "El usuario tiene un equipo activo" })
    });

    await expect(
      changeUserRole("u1", "Operador", "admin-token", fetchMock as unknown as typeof fetch)
    ).rejects.toMatchObject({ statusCode: 409, message: "El usuario tiene un equipo activo" });
  });
});

describe("getEquipos", () => {
  beforeEach(() => {
    vi.resetModules();
    vi.unstubAllEnvs();
  });

  it("GET a /identity/teams con bearer y devuelve la lista", async () => {
    vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test");
    const { getEquipos } = await import("./identityApi");
    const equipos = [
      {
        equipoId: "e1",
        nombreEquipo: "Los Halcones",
        estado: "Activo",
        participantes: [{ usuarioId: "u1", nombre: "Ana", esLider: true }]
      }
    ];
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => equipos
    });

    const result = await getEquipos("tok", fetchMock as unknown as typeof fetch);

    expect(fetchMock).toHaveBeenCalledWith("https://gw.example.test/identity/teams", {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
        Authorization: "Bearer tok"
      }
    });
    expect(result).toEqual(equipos);
  });

  it("lanza IdentityApiError en error HTTP", async () => {
    vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test");
    const { getEquipos } = await import("./identityApi");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: false,
      status: 403,
      json: async () => ({ message: "prohibido" })
    });

    await expect(getEquipos("tok", fetchMock as unknown as typeof fetch)).rejects.toMatchObject({
      name: "IdentityApiError",
      statusCode: 403
    });
  });
});
