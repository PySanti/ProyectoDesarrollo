import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { TriviaRuntimePanel } from "./TriviaRuntimePanel";
import {
  avanzarPregunta,
  finalizarJuegoActual,
  getPreguntaActual,
  OperacionesApiError,
  type PreguntaActualDto
} from "../../api/operacionesApi";
import { getRankingJuego, type RankingJuegoDto } from "../../api/puntuacionesApi";
import type { PreguntaDetail } from "../../api/partidasApi";

vi.mock("../../api/operacionesApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../api/operacionesApi")>();
  return { ...actual, getPreguntaActual: vi.fn(), avanzarPregunta: vi.fn(), finalizarJuegoActual: vi.fn() };
});
vi.mock("../../api/puntuacionesApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../api/puntuacionesApi")>();
  return { ...actual, getRankingJuego: vi.fn() };
});

const pregunta: PreguntaActualDto = {
  partidaId: "p1",
  juegoId: "j1",
  preguntaId: "q1",
  orden: 1,
  texto: "2+2?",
  tiempoLimiteSegundos: 30,
  fechaActivacion: new Date().toISOString(),
  opciones: [
    { opcionId: "o1", texto: "4" },
    { opcionId: "o2", texto: "5" }
  ]
};
const config: PreguntaDetail[] = [
  {
    preguntaId: "q1",
    texto: "2+2?",
    puntajeAsignado: 10,
    tiempoLimiteSegundos: 30,
    opciones: [
      { opcionId: "o1", texto: "4", esCorrecta: true },
      { opcionId: "o2", texto: "5", esCorrecta: false }
    ]
  }
];
const ranking: RankingJuegoDto = {
  juegoId: "j1",
  tipoJuego: "Trivia",
  generadoEn: "2026-07-08T12:00:00Z",
  entradas: [
    { posicion: 1, competidorId: "abcdef12-0000-0000-0000-000000000000", tipoCompetidor: "Participante", puntos: 10, tiempoAcumuladoMs: 61000, unidadesGanadas: 1 }
  ]
};

function renderPanel(props: Partial<Parameters<typeof TriviaRuntimePanel>[0]> = {}) {
  return render(
    <TriviaRuntimePanel
      partidaId="p1"
      juegoId="j1"
      accessToken="tok"
      preguntasConfig={config}
      puedeOperar={true}
      refetchSignal={0}
      onTerminada={vi.fn()}
      onJuegoAvanzado={vi.fn()}
      {...props}
    />
  );
}

describe("TriviaRuntimePanel", () => {
  beforeEach(() => {
    vi.mocked(getPreguntaActual).mockReset();
    vi.mocked(avanzarPregunta).mockReset();
    vi.mocked(finalizarJuegoActual).mockReset();
    vi.mocked(getRankingJuego).mockReset();
  });

  it("con pregunta activa muestra texto, opciones, correcta marcada, countdown y ranking", async () => {
    vi.mocked(getPreguntaActual).mockResolvedValue(pregunta);
    vi.mocked(getRankingJuego).mockResolvedValue(ranking);
    renderPanel();

    expect(await screen.findByTestId("pregunta-activa")).toBeInTheDocument();
    expect(screen.getByText("2+2?")).toBeInTheDocument();
    expect(screen.getByTestId("opcion-correcta")).toHaveTextContent("4");
    expect(screen.getByTestId("pregunta-countdown")).toBeInTheDocument();
    const tabla = screen.getByTestId("ranking-juego");
    expect(tabla).toHaveTextContent("abcdef12");
    expect(tabla).toHaveTextContent("10");
    expect(tabla).toHaveTextContent("01:01");
  });

  it("con 409 muestra sin-pregunta-activa y Finalizar juego; terminada llama onTerminada", async () => {
    vi.mocked(getPreguntaActual).mockRejectedValue(new OperacionesApiError("sin pregunta", 409));
    vi.mocked(getRankingJuego).mockResolvedValue(ranking);
    vi.mocked(finalizarJuegoActual).mockResolvedValue({
      partidaId: "p1", estado: "Terminada", juegoFinalizadoOrden: 1, juegoActivadoOrden: null, terminada: true
    });
    const onTerminada = vi.fn();
    renderPanel({ onTerminada });

    expect(await screen.findByTestId("sin-pregunta-activa")).toBeInTheDocument();
    await userEvent.click(screen.getByTestId("btn-finalizar-juego"));
    expect(onTerminada).toHaveBeenCalled();
  });

  it("finalizar que activa el siguiente juego llama onJuegoAvanzado", async () => {
    vi.mocked(getPreguntaActual).mockRejectedValue(new OperacionesApiError("sin pregunta", 409));
    vi.mocked(getRankingJuego).mockResolvedValue(ranking);
    vi.mocked(finalizarJuegoActual).mockResolvedValue({
      partidaId: "p1", estado: "Iniciada", juegoFinalizadoOrden: 1, juegoActivadoOrden: 2, terminada: false
    });
    const onJuegoAvanzado = vi.fn();
    renderPanel({ onJuegoAvanzado });

    await userEvent.click(await screen.findByTestId("btn-finalizar-juego"));
    expect(onJuegoAvanzado).toHaveBeenCalled();
  });

  it("avanzar pregunta refetchea la pregunta", async () => {
    vi.mocked(getPreguntaActual)
      .mockResolvedValueOnce(pregunta)
      .mockResolvedValue({ ...pregunta, preguntaId: "q2", orden: 2, texto: "3+3?" });
    vi.mocked(getRankingJuego).mockResolvedValue(ranking);
    vi.mocked(avanzarPregunta).mockResolvedValue({
      partidaId: "p1", preguntaCerradaOrden: 1, preguntaActivadaOrden: 2, sinMasPreguntas: false
    });
    renderPanel();

    await userEvent.click(await screen.findByTestId("btn-avanzar-pregunta"));
    expect(await screen.findByText("3+3?")).toBeInTheDocument();
  });

  it("ranking 404 muestra leyenda sin datos", async () => {
    vi.mocked(getPreguntaActual).mockResolvedValue(pregunta);
    const { PuntuacionesApiError } = await import("../../api/puntuacionesApi");
    vi.mocked(getRankingJuego).mockRejectedValue(new PuntuacionesApiError("no proyectado", 404));
    renderPanel();

    expect(await screen.findByText(/sin datos de ranking/i)).toBeInTheDocument();
  });

  it("cambio de refetchSignal refetchea", async () => {
    vi.mocked(getPreguntaActual).mockResolvedValue(pregunta);
    vi.mocked(getRankingJuego).mockResolvedValue(ranking);
    const { rerender } = renderPanel();
    await screen.findByTestId("pregunta-activa");
    expect(vi.mocked(getPreguntaActual)).toHaveBeenCalledTimes(1);

    rerender(
      <TriviaRuntimePanel
        partidaId="p1" juegoId="j1" accessToken="tok" preguntasConfig={config} puedeOperar={true}
        refetchSignal={1} onTerminada={vi.fn()} onJuegoAvanzado={vi.fn()}
      />
    );
    await vi.waitFor(() => expect(vi.mocked(getPreguntaActual)).toHaveBeenCalledTimes(2));
  });

  it("con dos opciones de igual texto solo marca la del opcionId correcto (F-B)", async () => {
    const preguntaDup: PreguntaActualDto = {
      ...pregunta,
      opciones: [
        { opcionId: "o1", texto: "4" },
        { opcionId: "o2", texto: "4" }
      ]
    };
    const configDup: PreguntaDetail[] = [
      {
        preguntaId: "q1",
        texto: "2+2?",
        puntajeAsignado: 10,
        tiempoLimiteSegundos: 30,
        opciones: [
          { opcionId: "o1", texto: "4", esCorrecta: true },
          { opcionId: "o2", texto: "4", esCorrecta: false }
        ]
      }
    ];
    vi.mocked(getPreguntaActual).mockResolvedValue(preguntaDup);
    vi.mocked(getRankingJuego).mockResolvedValue(ranking);
    renderPanel({ preguntasConfig: configDup });

    await screen.findByTestId("pregunta-activa");
    expect(screen.getAllByTestId("opcion-correcta")).toHaveLength(1);
  });

  it("con pregunta activa oculta btn-avanzar-pregunta cuando puedeOperar es false", async () => {
    vi.mocked(getPreguntaActual).mockResolvedValue(pregunta);
    vi.mocked(getRankingJuego).mockResolvedValue(ranking);
    renderPanel({ puedeOperar: false });

    expect(await screen.findByTestId("pregunta-activa")).toBeInTheDocument();
    expect(screen.queryByTestId("btn-avanzar-pregunta")).toBeNull();
  });

  it("sin pregunta activa oculta btn-finalizar-juego cuando puedeOperar es false", async () => {
    vi.mocked(getPreguntaActual).mockRejectedValue(new OperacionesApiError("sin pregunta", 409));
    vi.mocked(getRankingJuego).mockResolvedValue(ranking);
    renderPanel({ puedeOperar: false });

    expect(await screen.findByTestId("sin-pregunta-activa")).toBeInTheDocument();
    expect(screen.queryByTestId("btn-finalizar-juego")).toBeNull();
  });
});
