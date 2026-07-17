import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { CreateUserPage } from "./CreateUserPage";
import * as identityApi from "../../api/identityApi";

describe("CreateUserPage", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("renders the form", () => {
    render(<CreateUserPage accessToken="token" />);

    expect(screen.getByRole("heading", { name: /crear usuario/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/nombre/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/correo/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/rol inicial/i)).toBeInTheDocument();
  });

  it("submits and shows success", async () => {
    const spy = vi
      .spyOn(identityApi, "createIdentityUser")
      .mockResolvedValue({
        userId: "u1",
        keycloakId: "k1",
        name: "Ana",
        email: "ana@test.com",
        role: "Participante",
        status: "Activo"
      });

    render(<CreateUserPage accessToken="token-1" />);

    await userEvent.type(screen.getByLabelText(/nombre/i), "Ana");
    await userEvent.type(screen.getByLabelText(/correo/i), "ana@test.com");
    await userEvent.click(screen.getByRole("button", { name: /crear usuario/i }));

    expect(spy).toHaveBeenCalledTimes(1);
    expect(await screen.findByTestId("create-success")).toBeInTheDocument();
  });

  it("shows a live error and blocks submit when the name has no letters", async () => {
    const spy = vi.spyOn(identityApi, "createIdentityUser");

    render(<CreateUserPage accessToken="token-1" />);

    await userEvent.type(screen.getByLabelText(/nombre/i), "****");

    // Error en vivo (formato) desde la primera tecla, valor no vacio.
    expect(await screen.findByText(/al menos una letra/i)).toBeInTheDocument();

    await userEvent.type(screen.getByLabelText(/correo/i), "ana@test.com");
    await userEvent.click(screen.getByRole("button", { name: /crear usuario/i }));

    expect(spy).not.toHaveBeenCalled();
  });

  it("paints backend per-field errors from a 400 ValidationProblemDetails", async () => {
    vi.spyOn(identityApi, "createIdentityUser").mockRejectedValue(
      new identityApi.IdentityApiError("Solicitud invalida.", 400, {
        name: "Debe contener al menos una letra."
      })
    );

    render(<CreateUserPage accessToken="token-1" />);

    await userEvent.type(screen.getByLabelText(/nombre/i), "Ana");
    await userEvent.type(screen.getByLabelText(/correo/i), "ana@test.com");
    await userEvent.click(screen.getByRole("button", { name: /crear usuario/i }));

    expect(await screen.findByText(/al menos una letra/i)).toBeInTheDocument();
  });

  it("shows duplicate email message on 409", async () => {
    vi.spyOn(identityApi, "createIdentityUser").mockRejectedValue(
      new identityApi.IdentityApiError("duplicate", 409)
    );

    render(<CreateUserPage accessToken="token-1" />);

    await userEvent.type(screen.getByLabelText(/nombre/i), "Ana");
    await userEvent.type(screen.getByLabelText(/correo/i), "ana@test.com");
    await userEvent.click(screen.getByRole("button", { name: /crear usuario/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent("El correo ya existe en UMBRAL o Keycloak.");
  });

  it("shows welcome-email failure message and notes the user was not created on 502", async () => {
    vi.spyOn(identityApi, "createIdentityUser").mockRejectedValue(
      new identityApi.IdentityApiError("Failed to send welcome email to ana@test.com.", 502)
    );

    render(<CreateUserPage accessToken="token-1" />);

    await userEvent.type(screen.getByLabelText(/nombre/i), "Ana");
    await userEvent.type(screen.getByLabelText(/correo/i), "ana@test.com");
    await userEvent.click(screen.getByRole("button", { name: /crear usuario/i }));

    const alert = await screen.findByRole("alert");
    expect(alert).toHaveTextContent(/correo de bienvenida/i);
    expect(alert).toHaveTextContent(/El usuario no fue creado/i);
  });
});
