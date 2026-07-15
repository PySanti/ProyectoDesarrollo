import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { PartidaDetailPage } from "./PartidaDetailPage";
import { getPartida, PartidasApiError, type PartidaDetail } from "../../api/partidasApi";
import { publicarPartida, OperacionesApiError } from "../../api/operacionesApi";

vi.mock("../../api/partidasApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../api/partidasApi")>();
  return { ...actual, getPartida: vi.fn() };
});

vi.mock("../../api/operacionesApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../api/operacionesApi")>();
  return { ...actual, publicarPartida: vi.fn() };
});

const getPartidaMock = vi.mocked(getPartida);

function renderPage({
  partidaId = "p1",
  puedeOperar = true
}: { partidaId?: string; puedeOperar?: boolean } = {}) {
  return render(
    <MemoryRouter initialEntries={[`/partidas/${partidaId}`]}>
      <Routes>
        <Route
          path="/partidas/:partidaId"
          element={<PartidaDetailPage accessToken="token" puedeOperar={puedeOperar} />}
        />
      </Routes>
    </MemoryRouter>
  );
}

function renderPageConSesion({
  partidaId = "p1",
  puedeOperar = true
}: { partidaId?: string; puedeOperar?: boolean } = {}) {
  return render(
    <MemoryRouter initialEntries={[`/partidas/${partidaId}`]}>
      <Routes>
        <Route
          path="/partidas/:partidaId"
          element={<PartidaDetailPage accessToken="token" puedeOperar={puedeOperar} />}
        />
        <Route path="/partidas/:partidaId/sesion" element={<div>CONSOLA SESION</div>} />
      </Routes>
    </MemoryRouter>
  );
}

const detail: PartidaDetail = {
  partidaId: "p1",
  nombrePartida: "Trivia de verano",
  modalidad: "Individual",
  modoInicioPartida: "Manual",
  tiempoInicio: null,
  minimosParticipacion: 1,
  maximosParticipacion: 10,
  estado: "Lobby",
  juegos: [
    {
      juegoId: "j2",
      orden: 2,
      tipoJuego: "BusquedaDelTesoro",
      estado: "Pendiente",
      trivia: null,
      bdt: {
        areaBusqueda: "Plaza central",
        etapas: [
          {
            etapaBDTId: "e1",
            orden: 1,
            codigoQREsperado: "TESORO-1",
            puntajeAsignado: 50,
            tiempoLimiteSegundos: 60
          }
        ]
      }
    },
    {
      juegoId: "j1",
      orden: 1,
      tipoJuego: "Trivia",
      estado: "Pendiente",
      trivia: {
        preguntas: [
          {
            preguntaId: "q1",
            texto: "2+2?",
            puntajeAsignado: 100,
            tiempoLimiteSegundos: 30,
            opciones: [
              { opcionId: "o1", texto: "4", esCorrecta: true },
              { opcionId: "o2", texto: "5", esCorrecta: false }
            ]
          }
        ]
      },
      bdt: null
    }
  ]
};

describe("PartidaDetailPage", () => {
  beforeEach(() => {
    getPartidaMock.mockReset();
  });

  it("muestra ambos juegos en orden, la opcion correcta y la etapa BDT", async () => {
    getPartidaMock.mockResolvedValueOnce(detail);
    renderPage();

    expect(await screen.findByTestId("detalle-partida")).toBeInTheDocument();
    expect(screen.getByText("Trivia de verano")).toBeInTheDocument();

    const juego1 = screen.getByTestId("juego-1");
    const juego2 = screen.getByTestId("juego-2");

    expect(within(juego1).getByText("2+2?")).toBeInTheDocument();
    expect(within(juego1).getByText("4")).toBeInTheDocument();
    expect(within(juego1).getByText("Correcta")).toBeInTheDocument();

    expect(within(juego2).getByText("Plaza central")).toBeInTheDocument();
    expect(within(juego2).getByText("TESORO-1")).toBeInTheDocument();

    // juego-1 (Trivia) debe aparecer antes que juego-2 (BDT) pese a venir en otro orden en la respuesta.
    const orderedTestIds = screen
      .getAllByTestId(/^juego-/)
      .map((el) => el.getAttribute("data-testid"));
    expect(orderedTestIds).toEqual(["juego-1", "juego-2"]);
  });

  it("muestra 'Partida no encontrada' y un link a la lista cuando la API responde 404", async () => {
    getPartidaMock.mockRejectedValueOnce(new PartidasApiError("not found", 404));
    renderPage();

    expect(await screen.findByText("Partida no encontrada")).toBeInTheDocument();
    const link = screen.getByRole("link", { name: /partidas/i });
    expect(link).toHaveAttribute("href", "/partidas");
  });

  it("publicar y operar publica y navega a la consola de sesion", async () => {
    getPartidaMock.mockResolvedValueOnce(detail);
    vi.mocked(publicarPartida).mockResolvedValueOnce({
      partidaId: "p1",
      sesionPartidaId: "s1",
      estado: "Lobby",
      modalidad: "Individual",
      minimosParticipacion: 1,
      maximosParticipacion: 10,
      inscritosActivos: 0,
      participantes: [],
      equipos: [],
      solicitudesPendientesIndividual: [],
      solicitudesPendientesEquipo: []
    });
    renderPageConSesion();
    await screen.findByTestId("detalle-partida");
    await userEvent.click(screen.getByTestId("btn-publicar-operar"));
    expect(vi.mocked(publicarPartida)).toHaveBeenCalledWith("p1", "token");
    expect(await screen.findByText("CONSOLA SESION")).toBeInTheDocument();
  });

  it("si la partida ya estaba publicada (409) igual navega a la consola", async () => {
    getPartidaMock.mockResolvedValueOnce(detail);
    vi.mocked(publicarPartida).mockRejectedValueOnce(new OperacionesApiError("ya publicada", 409));
    renderPageConSesion();
    await screen.findByTestId("detalle-partida");
    await userEvent.click(screen.getByTestId("btn-publicar-operar"));
    expect(await screen.findByText("CONSOLA SESION")).toBeInTheDocument();
  });

  it("oculta 'Publicar y operar' cuando puedeOperar es false", async () => {
    getPartidaMock.mockResolvedValueOnce(detail);
    renderPage({ puedeOperar: false });

    expect(await screen.findByText(/historial de eventos/i)).toBeInTheDocument();
    expect(screen.queryByTestId("btn-publicar-operar")).toBeNull();
  });
});
