import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { PublishedBdtGamesPage } from "./PublishedBdtGamesPage";
import * as bdtApi from "../../api/bdtApi";

describe("PublishedBdtGamesPage", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("renders published BDT games for HU-37", async () => {
    vi.spyOn(bdtApi, "getOperatorPublishedBdtGames").mockResolvedValue([
      {
        partidaId: "p1",
        nombre: "Busqueda QR Campus",
        modalidad: "Individual",
        estado: "Lobby",
        areaBusqueda: "Patio central",
        cantidadEtapas: 2
      },
      {
        partidaId: "p2",
        nombre: "Busqueda Equipos",
        modalidad: "Equipo",
        estado: "Lobby",
        areaBusqueda: "Biblioteca",
        cantidadEtapas: 3
      }
    ]);

    render(<PublishedBdtGamesPage accessToken="operator-token" />);

    expect(screen.getByText(/cargando partidas bdt publicadas/i)).toBeInTheDocument();
    expect(await screen.findByText("Busqueda QR Campus")).toBeInTheDocument();
    expect(screen.getByText("Busqueda Equipos")).toBeInTheDocument();
    expect(screen.getAllByText("Lobby")).toHaveLength(2);
    expect(screen.getByText("Individual")).toBeInTheDocument();
    expect(screen.getByText("Equipo")).toBeInTheDocument();
    expect(screen.getByText("Patio central")).toBeInTheDocument();
    expect(screen.getByText("Biblioteca")).toBeInTheDocument();
    expect(screen.getByRole("table", { name: /partidas bdt publicadas para operador/i })).toBeInTheDocument();
  });

  it("renders empty state when there are no published games", async () => {
    vi.spyOn(bdtApi, "getOperatorPublishedBdtGames").mockResolvedValue([]);

    render(<PublishedBdtGamesPage accessToken="operator-token" />);

    expect(await screen.findByTestId("bdt-published-empty")).toHaveTextContent("No hay partidas BDT publicadas.");
  });

  it("renders error state when backend query fails", async () => {
    vi.spyOn(bdtApi, "getOperatorPublishedBdtGames").mockRejectedValue(
      new bdtApi.BdtApiError("failure", 500)
    );

    render(<PublishedBdtGamesPage accessToken="operator-token" />);

    expect(await screen.findByRole("alert")).toHaveTextContent("Error de persistencia al consultar BDT Game Service.");
  });

  it("renders unauthenticated state when backend returns 401", async () => {
    vi.spyOn(bdtApi, "getOperatorPublishedBdtGames").mockRejectedValue(
      new bdtApi.BdtApiError("unauthorized", 401)
    );

    render(<PublishedBdtGamesPage accessToken="expired-token" />);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Sesion expirada o no autenticada. Inicia sesion nuevamente."
    );
  });

  it("uses operator token when loading published games", async () => {
    const spy = vi.spyOn(bdtApi, "getOperatorPublishedBdtGames").mockResolvedValue([]);

    render(<PublishedBdtGamesPage accessToken="operator-token" />);

    await waitFor(() => {
      expect(spy).toHaveBeenCalledWith("operator-token");
    });
  });
});
