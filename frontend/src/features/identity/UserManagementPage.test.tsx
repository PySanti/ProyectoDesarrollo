import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
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
});
