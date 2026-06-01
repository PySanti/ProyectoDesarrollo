import { describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import * as identityApi from "../api/identityApi";

const { initMock } = vi.hoisted(() => ({
  initMock: vi.fn()
}));

vi.mock("../auth/keycloak", () => {
  return {
    authProvider: {
      init: initMock,
      logout: vi.fn()
    }
  };
});

import { App } from "./App";

describe("App auth guard", () => {
  it("blocks non-admin users", async () => {
    initMock.mockResolvedValueOnce({
      username: "operador",
      roles: ["Operador"],
      token: "token"
    });

    render(<App />);

    await waitFor(() => {
      expect(screen.getByText(/acceso restringido/i)).toBeInTheDocument();
    });
  });

  it("shows form for admin users", async () => {
    vi.spyOn(identityApi, "getIdentityUsers").mockResolvedValue([]);

    initMock.mockResolvedValueOnce({
      username: "admin",
      roles: ["Administrador"],
      token: "token"
    });

    render(<App />);

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /crear usuario/i })).toBeInTheDocument();
    });

    await userEvent.click(screen.getByRole("button", { name: /hu-02 gestionar usuarios/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /gestion de usuarios/i })).toBeInTheDocument();
    });
  });
});
