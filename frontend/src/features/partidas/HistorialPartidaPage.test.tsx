import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { HistorialPartidaPage } from "./HistorialPartidaPage";
import * as puntuacionesApi from "../../api/puntuacionesApi";
import * as partidasApi from "../../api/partidasApi";
import { PuntuacionesApiError } from "../../api/puntuacionesApi";
import { resetNombresCache } from "../shared/useNombres";

// juegoOrden/tipoJuego en null a propósito: este fixture ejerce el lag de proyección,
// donde hay juegoId pero aún no se conoce su orden y la columna cae al GUID corto.
const historial = {
  partidaId: "p1",
  total: 150,
  entradas: [
    {
      occurredAt: "2026-07-10T12:00:00Z",
      tipoEvento: "EtapaBDTGanada",
      juegoId: "abcdef12-0000-0000-0000-000000000000",
      participanteId: "11223344-0000-0000-0000-000000000000",
      equipoId: null,
      detalle: { puntaje: 50 },
      juegoOrden: null,
      tipoJuego: null
    }
  ]
};

function renderPage() {
  return render(
    <MemoryRouter initialEntries={["/partidas/p1/historial"]}>
      <Routes>
        <Route path="/partidas/:partidaId/historial" element={<HistorialPartidaPage accessToken="tok" />} />
        <Route path="/partidas/:partidaId" element={<p>pantalla de detalle</p>} />
      </Routes>
    </MemoryRouter>
  );
}

beforeEach(() => resetNombresCache());
afterEach(() => vi.restoreAllMocks());

describe("HistorialPartidaPage", () => {
  it("la cabecera muestra el nombre de la partida", async () => {
    vi.spyOn(puntuacionesApi, "getHistorialPartida").mockResolvedValue(historial);
    vi.spyOn(partidasApi, "getPartidas").mockResolvedValue([
      { partidaId: "p1", nombrePartida: "Copa UCAB" } as never
    ]);
    renderPage();

    expect(await screen.findByText(/Copa UCAB/)).toBeInTheDocument();
  });

  it("la columna Juego muestra orden y tipo, no el GUID", async () => {
    vi.spyOn(puntuacionesApi, "getHistorialPartida").mockResolvedValue({
      partidaId: "p1",
      total: 1,
      entradas: [
        {
          occurredAt: "2026-07-08T12:00:00Z",
          tipoEvento: "RespuestaTriviaValidada",
          juegoId: "abcdef12-0000-0000-0000-000000000000",
          participanteId: null,
          equipoId: null,
          detalle: {},
          juegoOrden: 1,
          tipoJuego: "Trivia"
        }
      ]
    });
    renderPage();

    expect(await screen.findByText("Juego 1 · Trivia")).toBeInTheDocument();
    expect(screen.queryByText("abcdef12")).not.toBeInTheDocument();
  });

  it("un evento de partida sin juego muestra raya", async () => {
    vi.spyOn(puntuacionesApi, "getHistorialPartida").mockResolvedValue({
      partidaId: "p1",
      total: 1,
      entradas: [
        {
          occurredAt: "2026-07-08T12:00:00Z",
          tipoEvento: "PartidaIniciada",
          juegoId: null,
          participanteId: null,
          equipoId: null,
          detalle: {},
          juegoOrden: null,
          tipoJuego: null
        }
      ]
    });
    renderPage();

    await screen.findByTestId("tabla-historial");
    // Cuatro columnas vacías en la misma fila: juego, participante, equipo y detalle
    // (este fixture no trae detalle; antes esa celda pintaba un "{}" que no decía nada).
    expect(screen.getAllByText("—")).toHaveLength(4);
  });

  it("muestra la tabla con eventos y el rango de paginación", async () => {
    vi.spyOn(puntuacionesApi, "getHistorialPartida").mockResolvedValue(historial);
    renderPage();
    const tabla = await screen.findByTestId("tabla-historial");
    // within: la etiqueta del evento ahora es la misma en la tabla y en el <option>.
    expect(within(tabla).getByText("Etapa BDT ganada")).toBeInTheDocument();
    expect(screen.getByText("abcdef12")).toBeInTheDocument();
    expect(screen.getByText(/1–1 de 150/)).toBeInTheDocument();
  });

  it("el filtro deja aislar los eventos de inscripcion que el backend proyecta", async () => {
    const spy = vi.spyOn(puntuacionesApi, "getHistorialPartida").mockResolvedValue(historial);
    renderPage();
    await screen.findByTestId("tabla-historial");
    await userEvent.selectOptions(
      screen.getByLabelText("Filtrar por tipo de evento"),
      "InscripcionAceptada"
    );
    const ultima = spy.mock.calls[spy.mock.calls.length - 1];
    expect(ultima[2]).toMatchObject({ tipo: "InscripcionAceptada" });
  });

  it("la columna Detalle se lee en claro, sin JSON crudo", async () => {
    vi.spyOn(puntuacionesApi, "getHistorialPartida").mockResolvedValue(historial);
    renderPage();
    const celda = await screen.findByTestId("detalle-evento");
    expect(celda).toHaveTextContent("Puntaje");
    expect(celda).toHaveTextContent("50");
    expect(celda.textContent).not.toContain('{"puntaje":50}');
  });

  it("un evento sin detalle no deja la celda en blanco", async () => {
    vi.spyOn(puntuacionesApi, "getHistorialPartida").mockResolvedValue({
      ...historial,
      entradas: [{ ...historial.entradas[0], detalle: {} }]
    });
    renderPage();
    expect(await screen.findByTestId("detalle-evento")).toHaveTextContent("—");
  });

  it("'Volver a la partida' es un boton secundario que navega al detalle", async () => {
    vi.spyOn(puntuacionesApi, "getHistorialPartida").mockResolvedValue(historial);
    renderPage();
    const volver = await screen.findByRole("button", { name: "Volver a la partida" });
    expect(volver).toHaveClass("secondary-button");
    await userEvent.click(volver);
    expect(await screen.findByText("pantalla de detalle")).toBeInTheDocument();
  });

  it("la paginacion usa el estilo secundario, no el primario de marca", async () => {
    vi.spyOn(puntuacionesApi, "getHistorialPartida").mockResolvedValue(historial);
    renderPage();
    await screen.findByTestId("tabla-historial");
    expect(screen.getByText("Anterior")).toHaveClass("secondary-button");
    expect(screen.getByText("Siguiente")).toHaveClass("secondary-button");
  });

  it("cambiar el filtro de tipo resetea offset y re-consulta con tipo", async () => {
    const spy = vi.spyOn(puntuacionesApi, "getHistorialPartida").mockResolvedValue(historial);
    renderPage();
    await screen.findByTestId("tabla-historial");
    await userEvent.click(screen.getByText("Siguiente"));
    await userEvent.selectOptions(
      screen.getByLabelText("Filtrar por tipo de evento"),
      "PistaEnviada"
    );
    const ultima = spy.mock.calls[spy.mock.calls.length - 1];
    expect(ultima[2]).toMatchObject({ offset: 0, tipo: "PistaEnviada" });
  });

  it("404 muestra el mensaje de proyección", async () => {
    vi.spyOn(puntuacionesApi, "getHistorialPartida").mockRejectedValue(
      new PuntuacionesApiError("no existe", 404)
    );
    renderPage();
    expect(
      await screen.findByText("La partida no existe en la proyección de Puntuaciones.")
    ).toBeInTheDocument();
  });

  it("200 sin eventos muestra un vacío que enseña, no una linea suelta", async () => {
    vi.spyOn(puntuacionesApi, "getHistorialPartida").mockResolvedValue({
      partidaId: "p1",
      total: 0,
      entradas: []
    });
    renderPage();
    const vacio = await screen.findByTestId("historial-vacio");
    expect(vacio).toHaveClass("empty-panel");
    expect(vacio).toHaveTextContent("Sin eventos registrados.");
  });

  it("el vacío por filtro explica que es el filtro y deja quitarlo", async () => {
    vi.spyOn(puntuacionesApi, "getHistorialPartida").mockResolvedValue({
      partidaId: "p1",
      total: 0,
      entradas: []
    });
    renderPage();
    await screen.findByTestId("historial-vacio");
    await userEvent.selectOptions(screen.getByLabelText("Filtrar por tipo de evento"), "PistaEnviada");
    expect(await screen.findByTestId("historial-vacio")).toHaveTextContent(/filtro/i);
  });
});
