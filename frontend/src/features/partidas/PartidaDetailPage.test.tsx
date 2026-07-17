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
            codigoQREsperado: "11111111-1111-1111-1111-111111111111",
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
    },
    // Segundo juego BDT: la partida no limita cuantos juegos BDT puede tener, y cada uno
    // numera sus propias etapas desde 1. Esta "etapa 1" del juego 3 es la que expone la
    // colision de nombreArchivoQr si el nombre del archivo solo mira el orden de la etapa.
    {
      juegoId: "j3",
      orden: 3,
      tipoJuego: "BusquedaDelTesoro",
      estado: "Pendiente",
      trivia: null,
      bdt: {
        areaBusqueda: "Sotano",
        etapas: [
          {
            etapaBDTId: "e2",
            orden: 1,
            codigoQREsperado: "22222222-2222-2222-2222-222222222222",
            puntajeAsignado: 30,
            tiempoLimiteSegundos: 45
          }
        ]
      }
    }
  ]
};

describe("PartidaDetailPage", () => {
  beforeEach(() => {
    getPartidaMock.mockReset();
  });

  it("muestra los tres juegos en orden, la opcion correcta y las etapas BDT", async () => {
    getPartidaMock.mockResolvedValueOnce(detail);
    renderPage();

    expect(await screen.findByTestId("detalle-partida")).toBeInTheDocument();
    expect(screen.getByText("Trivia de verano")).toBeInTheDocument();

    const juego1 = screen.getByTestId("juego-1");
    const juego2 = screen.getByTestId("juego-2");
    const juego3 = screen.getByTestId("juego-3");

    expect(within(juego1).getByText("2+2?")).toBeInTheDocument();
    expect(within(juego1).getByText("4")).toBeInTheDocument();
    expect(within(juego1).getByText("Correcta")).toBeInTheDocument();

    expect(within(juego2).getByText("Plaza central")).toBeInTheDocument();
    expect(within(juego2).getByText("11111111-1111-1111-1111-111111111111")).toBeInTheDocument();

    expect(within(juego3).getByText("Sotano")).toBeInTheDocument();
    expect(within(juego3).getByText("22222222-2222-2222-2222-222222222222")).toBeInTheDocument();

    // juego-1 (Trivia) debe aparecer antes que juego-2 y juego-3 (BDT) pese a venir en
    // otro orden en la respuesta.
    const orderedTestIds = screen
      .getAllByTestId(/^juego-/)
      .map((el) => el.getAttribute("data-testid"));
    expect(orderedTestIds).toEqual(["juego-1", "juego-2", "juego-3"]);
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

  it("muestra el QR de cada etapa para reimprimirlo, con nombre y alt distintos por juego", async () => {
    getPartidaMock.mockResolvedValueOnce(detail);
    renderPage();

    // Juego 2 y juego 3 son ambos BDT y cada uno tiene su propia "etapa 1": si el nombre
    // de archivo o el alt solo miraran el orden de la etapa, estas dos etapas serian
    // indistinguibles pese a tener codigos QR (tesoros) distintos.
    const juego2 = await screen.findByTestId("juego-2");
    const juego3 = screen.getByTestId("juego-3");

    // Los codigos del fixture ("11111111-..." y "22222222-...") difieren, y el nombre de
    // archivo debe incluir el prefijo de cada uno: es lo que garantiza que dos etapas con el
    // mismo juego+etapa (p.ej. tras un reordenamiento en el wizard antes de crear la partida)
    // nunca produzcan el mismo archivo, sin depender de que la posicion se mantenga estable.
    expect(
      await within(juego2).findByRole("img", { name: /qr del tesoro del juego 2, etapa 1/i })
    ).toBeInTheDocument();
    expect(within(juego2).getByRole("link", { name: /descargar qr etapa 1/i })).toHaveAttribute(
      "download",
      "tesoro-juego-2-etapa-1-11111111.png"
    );

    expect(
      await within(juego3).findByRole("img", { name: /qr del tesoro del juego 3, etapa 1/i })
    ).toBeInTheDocument();
    expect(within(juego3).getByRole("link", { name: /descargar qr etapa 1/i })).toHaveAttribute(
      "download",
      "tesoro-juego-3-etapa-1-22222222.png"
    );
  });

  it("no ofrece regenerar el QR", async () => {
    getPartidaMock.mockResolvedValueOnce(detail);
    renderPage();
    const juego2 = await screen.findByTestId("juego-2");
    await within(juego2).findByRole("img", { name: /qr del tesoro del juego 2, etapa 1/i });

    expect(screen.queryByRole("button", { name: /regenerar/i })).not.toBeInTheDocument();
  });

  it("el QR queda oculto tras un disclosure cerrado por defecto al entrar al detalle", async () => {
    getPartidaMock.mockResolvedValueOnce(detail);
    renderPage();
    const juego2 = await screen.findByTestId("juego-2");
    await within(juego2).findByRole("img", { name: /qr del tesoro del juego 2, etapa 1/i });

    const details = within(juego2).getByText("Mostrar QR").closest("details");
    expect(details).not.toBeNull();
    expect(details).not.toHaveAttribute("open");
  });
});
