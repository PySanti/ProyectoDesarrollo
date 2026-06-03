import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { CreateBdtGamePage } from "./CreateBdtGamePage";
import * as bdtApi from "../../api/bdtApi";

describe("CreateBdtGamePage", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("renders HU-34 BDT creation form", () => {
    render(<CreateBdtGamePage accessToken="token" />);

    expect(screen.getByRole("heading", { name: /crear partida bdt/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/nombre/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/area de busqueda/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/qr esperado/i)).toBeInTheDocument();
  });

  it("submits individual BDT creation and shows success", async () => {
    const spy = vi.spyOn(bdtApi, "createBdtGame").mockResolvedValue({
      partidaId: "p1",
      nombre: "Busqueda QR Campus",
      modalidad: "Individual",
      estado: "Lobby",
      areaBusqueda: "Patio central",
      modoInicio: "Manual",
      cantidadEtapas: 1
    });

    render(<CreateBdtGamePage accessToken="token-1" />);

    await userEvent.type(screen.getByLabelText(/nombre/i), "Busqueda QR Campus");
    await userEvent.type(screen.getByLabelText(/area de busqueda/i), "Patio central");
    await userEvent.type(screen.getByLabelText(/qr esperado/i), "QR-ETAPA-1");
    await userEvent.click(screen.getByRole("button", { name: /crear partida bdt/i }));

    expect(spy).toHaveBeenCalledWith(
      expect.objectContaining({
        nombre: "Busqueda QR Campus",
        modalidad: "Individual",
        maximoParticipantes: 10,
        maximoEquipos: null,
        etapas: [expect.objectContaining({ codigoQrEsperado: "QR-ETAPA-1" })]
      }),
      "token-1"
    );
    expect(await screen.findByTestId("bdt-create-success")).toBeInTheDocument();
  });

  it("shows validation error when stage QR is missing", async () => {
    render(<CreateBdtGamePage accessToken="token" />);

    await userEvent.type(screen.getByLabelText(/nombre/i), "Busqueda QR Campus");
    await userEvent.type(screen.getByLabelText(/area de busqueda/i), "Patio central");
    await userEvent.click(screen.getByRole("button", { name: /crear partida bdt/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent("El QR esperado de la etapa 1 es obligatorio.");
  });

  it("submits team BDT creation with team limits payload", async () => {
    const spy = vi.spyOn(bdtApi, "createBdtGame").mockResolvedValue({
      partidaId: "p2",
      nombre: "Busqueda Equipos",
      modalidad: "Equipo",
      estado: "Lobby",
      areaBusqueda: "Campus completo",
      modoInicio: "ManualYAutomatico",
      cantidadEtapas: 1
    });

    render(<CreateBdtGamePage accessToken="operator-token" />);

    await userEvent.type(screen.getByLabelText(/nombre/i), "Busqueda Equipos");
    await userEvent.type(screen.getByLabelText(/area de busqueda/i), "Campus completo");
    await userEvent.selectOptions(screen.getByLabelText(/modalidad/i), "Equipo");
    await userEvent.selectOptions(screen.getByLabelText(/modo de inicio/i), "ManualYAutomatico");
    await userEvent.type(screen.getByLabelText(/maximo equipos/i), "5");
    await userEvent.type(screen.getByLabelText(/minimo jugadores por equipo/i), "2");
    await userEvent.type(screen.getByLabelText(/qr esperado/i), "QR-EQUIPO-1");
    await userEvent.click(screen.getByRole("button", { name: /crear partida bdt/i }));

    expect(spy).toHaveBeenCalledWith(
      expect.objectContaining({
        modalidad: "Equipo",
        maximoParticipantes: null,
        maximoEquipos: 5,
        minimoJugadoresPorEquipo: 2,
        modoInicio: "ManualYAutomatico"
      }),
      "operator-token"
    );
    expect(await screen.findByTestId("bdt-create-success")).toBeInTheDocument();
  });

  it("submits multiple stages with deterministic order", async () => {
    const spy = vi.spyOn(bdtApi, "createBdtGame").mockResolvedValue({
      partidaId: "p3",
      nombre: "Busqueda Multi Etapa",
      modalidad: "Individual",
      estado: "Lobby",
      areaBusqueda: "Campus completo",
      modoInicio: "Manual",
      cantidadEtapas: 2
    });

    render(<CreateBdtGamePage accessToken="operator-token" />);

    await userEvent.type(screen.getByLabelText(/nombre/i), "Busqueda Multi Etapa");
    await userEvent.type(screen.getByLabelText(/area de busqueda/i), "Campus completo");
    await userEvent.type(screen.getByLabelText(/qr esperado etapa 1/i), "QR-ETAPA-1");
    await userEvent.clear(screen.getByLabelText(/tiempo limite segundos etapa 1/i));
    await userEvent.type(screen.getByLabelText(/tiempo limite segundos etapa 1/i), "180");
    await userEvent.click(screen.getByRole("button", { name: /agregar etapa/i }));
    await userEvent.type(screen.getByLabelText(/qr esperado etapa 2/i), "QR-ETAPA-2");
    await userEvent.clear(screen.getByLabelText(/tiempo limite segundos etapa 2/i));
    await userEvent.type(screen.getByLabelText(/tiempo limite segundos etapa 2/i), "240");
    await userEvent.click(screen.getByRole("button", { name: /crear partida bdt/i }));

    expect(spy).toHaveBeenCalledWith(
      expect.objectContaining({
        etapas: [
          { orden: 1, codigoQrEsperado: "QR-ETAPA-1", tiempoLimiteSegundos: 180 },
          { orden: 2, codigoQrEsperado: "QR-ETAPA-2", tiempoLimiteSegundos: 240 }
        ]
      }),
      "operator-token"
    );
    expect(await screen.findByTestId("bdt-create-success")).toHaveTextContent("2 etapa(s)");
  });

  it("removes a stage and submits remaining stages with contiguous order", async () => {
    const spy = vi.spyOn(bdtApi, "createBdtGame").mockResolvedValue({
      partidaId: "p4",
      nombre: "Busqueda Reordenada",
      modalidad: "Individual",
      estado: "Lobby",
      areaBusqueda: "Biblioteca",
      modoInicio: "Manual",
      cantidadEtapas: 2
    });

    render(<CreateBdtGamePage accessToken="operator-token" />);

    await userEvent.type(screen.getByLabelText(/nombre/i), "Busqueda Reordenada");
    await userEvent.type(screen.getByLabelText(/area de busqueda/i), "Biblioteca");
    await userEvent.type(screen.getByLabelText(/qr esperado etapa 1/i), "QR-ELIMINAR");
    await userEvent.click(screen.getByRole("button", { name: /agregar etapa/i }));
    await userEvent.type(screen.getByLabelText(/qr esperado etapa 2/i), "QR-UNO");
    await userEvent.click(screen.getByRole("button", { name: /agregar etapa/i }));
    await userEvent.type(screen.getByLabelText(/qr esperado etapa 3/i), "QR-DOS");
    await userEvent.click(screen.getByRole("button", { name: /eliminar etapa 1/i }));
    await userEvent.click(screen.getByRole("button", { name: /crear partida bdt/i }));

    expect(spy).toHaveBeenCalledWith(
      expect.objectContaining({
        etapas: [
          { orden: 1, codigoQrEsperado: "QR-UNO", tiempoLimiteSegundos: 300 },
          { orden: 2, codigoQrEsperado: "QR-DOS", tiempoLimiteSegundos: 300 }
        ]
      }),
      "operator-token"
    );
  });

  it("maps 409 backend conflict to modality limits message", async () => {
    vi.spyOn(bdtApi, "createBdtGame").mockRejectedValue(
      new bdtApi.BdtApiError("conflict", 409)
    );

    render(<CreateBdtGamePage accessToken="token" />);

    await userEvent.type(screen.getByLabelText(/nombre/i), "Busqueda QR Campus");
    await userEvent.type(screen.getByLabelText(/area de busqueda/i), "Patio central");
    await userEvent.type(screen.getByLabelText(/qr esperado/i), "QR-ETAPA-1");
    await userEvent.click(screen.getByRole("button", { name: /crear partida bdt/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent("La configuracion de modalidad y limites no es valida.");
  });
});
