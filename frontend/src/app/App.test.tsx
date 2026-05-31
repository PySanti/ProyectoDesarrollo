import { describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";

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
    initMock.mockResolvedValueOnce({
      username: "admin",
      roles: ["Administrador"],
      token: "token"
    });

    render(<App />);

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /crear usuario/i })).toBeInTheDocument();
    });
  });
});
