import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { EquiposPage } from "./EquiposPage";
import * as identityApi from "../../api/identityApi";

const equipos: identityApi.EquipoAdminItem[] = [
  {
    equipoId: "11111111-2222-3333-4444-555555555555",
    nombreEquipo: "Los Halcones",
    estado: "Activo",
    participantes: [
      { usuarioId: "u1", nombre: "Ana", esLider: true },
      { usuarioId: "u2", nombre: "Luis", esLider: false }
    ]
  },
  {
    equipoId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
    nombreEquipo: "Zorros",
    estado: "Eliminado",
    participantes: [{ usuarioId: "u3", nombre: "Eva", esLider: true }]
  }
];

function renderPage() {
  return render(
    <MemoryRouter>
      <EquiposPage accessToken="tok" />
    </MemoryRouter>
  );
}

afterEach(() => vi.restoreAllMocks());

describe("EquiposPage", () => {
  it("muestra la tabla con miembros, líder marcado y badge de estado", async () => {
    vi.spyOn(identityApi, "getEquipos").mockResolvedValue(equipos);
    renderPage();
    expect(await screen.findByTestId("tabla-equipos")).toBeInTheDocument();
    expect(screen.getByText("Los Halcones")).toBeInTheDocument();
    expect(screen.getByText("Ana (líder), Luis")).toBeInTheDocument();
    expect(screen.getByText("Eliminado")).toBeInTheDocument();
  });

  it("cada fila enlaza al rendimiento con su equipoId", async () => {
    vi.spyOn(identityApi, "getEquipos").mockResolvedValue(equipos);
    renderPage();
    const links = await screen.findAllByRole("link", { name: "Ver rendimiento" });
    expect(links[0]).toHaveAttribute(
      "href",
      "/puntuaciones/equipos?equipoId=11111111-2222-3333-4444-555555555555"
    );
  });

  it("lista vacía muestra el mensaje de vacío", async () => {
    vi.spyOn(identityApi, "getEquipos").mockResolvedValue([]);
    renderPage();
    expect(await screen.findByText("No hay equipos registrados.")).toBeInTheDocument();
  });

  it("error de la api muestra aviso con reintento", async () => {
    vi.spyOn(identityApi, "getEquipos")
      .mockRejectedValueOnce(new identityApi.IdentityApiError("prohibido", 403))
      .mockResolvedValueOnce(equipos);
    renderPage();
    expect(await screen.findByText("prohibido")).toBeInTheDocument();
    (await screen.findByRole("button", { name: "Reintentar" })).click();
    expect(await screen.findByTestId("tabla-equipos")).toBeInTheDocument();
  });
});
