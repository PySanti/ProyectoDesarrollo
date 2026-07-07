import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { UserManagementPage } from "./UserManagementPage";
import * as identityApi from "../../api/identityApi";

describe("UserManagementPage", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("loads list and allows detail + update happy path", async () => {
    vi.spyOn(identityApi, "getIdentityUsers").mockResolvedValue([
      {
        userId: "u1",
        keycloakId: "k1",
        name: "Ana",
        email: "ana@demo.com",
        role: "Participante",
        status: "Activo"
      }
    ]);

    vi.spyOn(identityApi, "getIdentityUserById").mockResolvedValue({
      userId: "u1",
      keycloakId: "k1",
      name: "Ana",
      email: "ana@demo.com",
      role: "Participante",
      status: "Activo"
    });

    const updateSpy = vi.spyOn(identityApi, "updateIdentityUserGeneralData").mockResolvedValue({
      userId: "u1",
      keycloakId: "k1",
      name: "Ana Maria",
      email: "ana.maria@demo.com",
      role: "Participante",
      status: "Activo"
    });

    render(<UserManagementPage accessToken="token" />);

    await userEvent.click(await screen.findByRole("button", { name: /ana/i }));
    // Anticipa al admin que cambiar el correo de un usuario sin primer login reenvía credenciales.
    expect(await screen.findByTestId("hu02-email-hint")).toHaveTextContent(
      /se le enviará un .*correo.* con su nueva contraseña temporal/i
    );
    await userEvent.clear(screen.getByLabelText(/nombre/i));
    await userEvent.type(screen.getByLabelText(/nombre/i), "Ana Maria");
    await userEvent.clear(screen.getByLabelText(/correo/i));
    await userEvent.type(screen.getByLabelText(/correo/i), "ana.maria@demo.com");
    await userEvent.click(screen.getByRole("button", { name: /guardar datos/i }));

    expect(updateSpy).toHaveBeenCalledTimes(1);
    expect(await screen.findByTestId("hu02-success")).toHaveTextContent(
      "Datos generales actualizados correctamente."
    );
  });

  it("maps 409 update error to duplicate email message", async () => {
    vi.spyOn(identityApi, "getIdentityUsers").mockResolvedValue([
      {
        userId: "u1",
        keycloakId: "k1",
        name: "Ana",
        email: "ana@demo.com",
        role: "Participante",
        status: "Activo"
      }
    ]);

    vi.spyOn(identityApi, "getIdentityUserById").mockResolvedValue({
      userId: "u1",
      keycloakId: "k1",
      name: "Ana",
      email: "ana@demo.com",
      role: "Participante",
      status: "Activo"
    });

    vi.spyOn(identityApi, "updateIdentityUserGeneralData").mockRejectedValue(
      new identityApi.IdentityApiError("duplicate", 409)
    );

    render(<UserManagementPage accessToken="token" />);

    await userEvent.click(await screen.findByRole("button", { name: /ana/i }));
    await userEvent.click(screen.getByRole("button", { name: /guardar datos/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent("El correo ya existe en UMBRAL o Keycloak.");
  });

  it("deactivates selected user and shows state feedback", async () => {
    vi.spyOn(identityApi, "getIdentityUsers").mockResolvedValue([
      {
        userId: "u1",
        keycloakId: "k1",
        name: "Ana",
        email: "ana@demo.com",
        role: "Participante",
        status: "Activo"
      }
    ]);

    vi.spyOn(identityApi, "getIdentityUserById").mockResolvedValue({
      userId: "u1",
      keycloakId: "k1",
      name: "Ana",
      email: "ana@demo.com",
      role: "Participante",
      status: "Activo"
    });

    vi.spyOn(identityApi, "deactivateIdentityUser").mockResolvedValue({
      userId: "u1",
      status: "Desactivado"
    });

    render(<UserManagementPage accessToken="token" />);

    await userEvent.click(await screen.findByRole("button", { name: /ana/i }));
    await userEvent.click(screen.getByRole("button", { name: /desactivar usuario/i }));

    expect(await screen.findByTestId("hu02-success")).toHaveTextContent(
      "Usuario desactivado correctamente."
    );
    expect(screen.getAllByText(/desactivado/i).length).toBeGreaterThan(0);
  });

  it("maps 403 list error to admin authorization message", async () => {
    vi.spyOn(identityApi, "getIdentityUsers").mockRejectedValue(
      new identityApi.IdentityApiError("forbidden", 403)
    );

    render(<UserManagementPage accessToken="token" />);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "No autorizado. Debes tener rol Administrador."
    );
  });

  it("maps 404 detail error to user not found message", async () => {
    vi.spyOn(identityApi, "getIdentityUsers").mockResolvedValue([
      {
        userId: "u1",
        keycloakId: "k1",
        name: "Ana",
        email: "ana@demo.com",
        role: "Participante",
        status: "Activo"
      }
    ]);

    vi.spyOn(identityApi, "getIdentityUserById").mockRejectedValue(
      new identityApi.IdentityApiError("not found", 404)
    );

    render(<UserManagementPage accessToken="token" />);

    await userEvent.click(await screen.findByRole("button", { name: /ana/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Usuario no encontrado.");
  });

  it("maps 500 deactivate error to fallback identity service message", async () => {
    vi.spyOn(identityApi, "getIdentityUsers").mockResolvedValue([
      {
        userId: "u1",
        keycloakId: "k1",
        name: "Ana",
        email: "ana@demo.com",
        role: "Participante",
        status: "Activo"
      }
    ]);

    vi.spyOn(identityApi, "getIdentityUserById").mockResolvedValue({
      userId: "u1",
      keycloakId: "k1",
      name: "Ana",
      email: "ana@demo.com",
      role: "Participante",
      status: "Activo"
    });

    vi.spyOn(identityApi, "deactivateIdentityUser").mockRejectedValue(
      new identityApi.IdentityApiError("", 500)
    );

    render(<UserManagementPage accessToken="token" />);

    await userEvent.click(await screen.findByRole("button", { name: /ana/i }));
    await userEvent.click(screen.getByRole("button", { name: /desactivar usuario/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Error inesperado en Identity Service."
    );
  });

  function mockListWith(users: identityApi.IdentityUserSummary[]) {
    vi.spyOn(identityApi, "getIdentityUsers").mockResolvedValue(users);
  }

  const ANA: identityApi.IdentityUserSummary = {
    userId: "u1",
    keycloakId: "k1",
    name: "Ana",
    email: "ana@demo.com",
    role: "Participante",
    status: "Activo"
  };

  const ROOT: identityApi.IdentityUserSummary = {
    userId: "u2",
    keycloakId: "k2",
    name: "Root",
    email: "root@demo.com",
    role: "Administrador",
    status: "Activo"
  };

  it("deshabilita Cambiar rol para un Administrador con title explicativo", async () => {
    mockListWith([ROOT]);

    render(<UserManagementPage accessToken="token" />);

    const button = await screen.findByTestId("role-change-open-u2");
    expect(button).toBeDisabled();
    expect(button).toHaveAttribute("title", "El rol de un Administrador es inmutable.");
  });

  it("cambio a rol no-admin en un click: llama API, actualiza fila y cierra modal", async () => {
    mockListWith([ANA]);
    const changeSpy = vi.spyOn(identityApi, "changeUserRole").mockResolvedValue({
      usuarioId: "u1",
      rol: "Operador"
    });

    render(<UserManagementPage accessToken="token" />);

    await userEvent.click(await screen.findByTestId("role-change-open-u1"));
    const select = screen.getByTestId("role-change-select");
    // El rol actual no es opción.
    expect(within(select).queryByRole("option", { name: "Participante" })).toBeNull();
    await userEvent.selectOptions(select, "Operador");
    await userEvent.click(screen.getByTestId("role-change-confirm"));

    expect(changeSpy).toHaveBeenCalledWith("u1", "Operador", "token");
    await waitFor(() => expect(screen.queryByTestId("role-change-modal")).toBeNull());
    expect(screen.getByRole("cell", { name: "Operador" })).toBeInTheDocument();
  });

  it("promover a Administrador exige segundo click y no llama a la API en el primero", async () => {
    mockListWith([ANA]);
    const changeSpy = vi.spyOn(identityApi, "changeUserRole").mockResolvedValue({
      usuarioId: "u1",
      rol: "Administrador"
    });

    render(<UserManagementPage accessToken="token" />);

    await userEvent.click(await screen.findByTestId("role-change-open-u1"));
    await userEvent.selectOptions(screen.getByTestId("role-change-select"), "Administrador");

    expect(screen.getByTestId("role-change-warning")).toHaveTextContent(/irreversible/i);

    await userEvent.click(screen.getByTestId("role-change-confirm"));
    expect(changeSpy).not.toHaveBeenCalled();
    expect(screen.getByTestId("role-change-confirm")).toHaveTextContent("Entiendo, promover");

    await userEvent.click(screen.getByTestId("role-change-confirm"));
    expect(changeSpy).toHaveBeenCalledWith("u1", "Administrador", "token");
  });

  it("409 del backend queda inline en el modal sin cerrarlo", async () => {
    mockListWith([ANA]);
    vi.spyOn(identityApi, "changeUserRole").mockRejectedValue(
      new identityApi.IdentityApiError("El usuario u1 tiene un equipo activo", 409)
    );

    render(<UserManagementPage accessToken="token" />);

    await userEvent.click(await screen.findByTestId("role-change-open-u1"));
    await userEvent.selectOptions(screen.getByTestId("role-change-select"), "Operador");
    await userEvent.click(screen.getByTestId("role-change-confirm"));

    expect(await screen.findByTestId("role-change-error")).toHaveTextContent(/equipo activo/i);
    expect(screen.getByTestId("role-change-modal")).toBeInTheDocument();
  });

  it("502 muestra mensaje de Keycloak inline", async () => {
    mockListWith([ANA]);
    vi.spyOn(identityApi, "changeUserRole").mockRejectedValue(
      new identityApi.IdentityApiError("bad gateway", 502)
    );

    render(<UserManagementPage accessToken="token" />);

    await userEvent.click(await screen.findByTestId("role-change-open-u1"));
    await userEvent.selectOptions(screen.getByTestId("role-change-select"), "Operador");
    await userEvent.click(screen.getByTestId("role-change-confirm"));

    expect(await screen.findByTestId("role-change-error")).toHaveTextContent(/keycloak no disponible/i);
  });
});
