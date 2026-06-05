import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { TriviaOperationsPage } from "./TriviaOperationsPage";
import * as triviaApi from "../../api/triviaApi";

describe("TriviaOperationsPage", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.spyOn(triviaApi, "getOperatorTriviaGamesForSupervision").mockResolvedValue([]);
  });

  it("creates a Trivia form with one complete question", async () => {
    const spy = vi.spyOn(triviaApi, "createTriviaForm").mockResolvedValue({
      id: "form-1",
      title: "Formulario demo",
      isComplete: true,
      questionsCount: 1,
      incompleteReasons: [],
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: "2026-01-01T00:00:00Z",
      questions: [
        {
          id: "q1",
          text: "Pregunta",
          assignedScore: 100,
          timeLimitSeconds: 30,
          displayOrder: 1,
          options: []
        }
      ]
    });

    render(<TriviaOperationsPage accessToken="operator-token" />);

    await userEvent.type(screen.getByLabelText(/titulo del formulario/i), "Formulario demo");
    await userEvent.type(screen.getByLabelText(/texto de pregunta 1/i), "Pregunta demo");
    await userEvent.type(screen.getByLabelText(/opcion a pregunta 1/i), "A");
    await userEvent.type(screen.getByLabelText(/opcion b pregunta 1/i), "B");
    await userEvent.type(screen.getByLabelText(/opcion c pregunta 1/i), "C");
    await userEvent.type(screen.getByLabelText(/opcion d pregunta 1/i), "D");
    await userEvent.click(screen.getByRole("button", { name: /crear formulario/i }));

    expect(spy).toHaveBeenCalledWith(
      expect.objectContaining({
        title: "Formulario demo",
        questions: [expect.objectContaining({ text: "Pregunta demo", assignedScore: 100 })]
      }),
      "operator-token"
    );
    expect(await screen.findByRole("status")).toHaveTextContent("Formulario creado");
  });

  it("creates a Trivia form with multiple questions in display order", async () => {
    const spy = vi.spyOn(triviaApi, "createTriviaForm").mockResolvedValue({
      id: "form-1",
      title: "Formulario multi",
      isComplete: true,
      questionsCount: 2,
      incompleteReasons: [],
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: "2026-01-01T00:00:00Z",
      questions: [
        {
          id: "q1",
          text: "Pregunta 1",
          assignedScore: 100,
          timeLimitSeconds: 30,
          displayOrder: 1,
          options: []
        },
        {
          id: "q2",
          text: "Pregunta 2",
          assignedScore: 200,
          timeLimitSeconds: 45,
          displayOrder: 2,
          options: []
        }
      ]
    });

    render(<TriviaOperationsPage accessToken="operator-token" />);

    await userEvent.type(screen.getByLabelText(/titulo del formulario/i), "Formulario multi");
    await userEvent.type(screen.getByLabelText(/texto de pregunta 1/i), "Pregunta 1");
    await userEvent.type(screen.getByLabelText(/opcion a pregunta 1/i), "A1");
    await userEvent.type(screen.getByLabelText(/opcion b pregunta 1/i), "B1");
    await userEvent.type(screen.getByLabelText(/opcion c pregunta 1/i), "C1");
    await userEvent.type(screen.getByLabelText(/opcion d pregunta 1/i), "D1");

    await userEvent.click(screen.getByRole("button", { name: /agregar pregunta/i }));
    await userEvent.type(screen.getByLabelText(/texto de pregunta 2/i), "Pregunta 2");
    await userEvent.type(screen.getByLabelText(/opcion a pregunta 2/i), "A2");
    await userEvent.type(screen.getByLabelText(/opcion b pregunta 2/i), "B2");
    await userEvent.type(screen.getByLabelText(/opcion c pregunta 2/i), "C2");
    await userEvent.type(screen.getByLabelText(/opcion d pregunta 2/i), "D2");
    await userEvent.clear(screen.getByLabelText(/puntaje pregunta 2/i));
    await userEvent.type(screen.getByLabelText(/puntaje pregunta 2/i), "200");
    await userEvent.clear(screen.getByLabelText(/tiempo limite pregunta 2/i));
    await userEvent.type(screen.getByLabelText(/tiempo limite pregunta 2/i), "45");
    await userEvent.selectOptions(screen.getByLabelText(/respuesta correcta pregunta 2/i), "2");

    await userEvent.click(screen.getByRole("button", { name: /crear formulario/i }));

    expect(spy).toHaveBeenCalledWith(
      expect.objectContaining({
        title: "Formulario multi",
        questions: [
          expect.objectContaining({ text: "Pregunta 1", displayOrder: 1 }),
          expect.objectContaining({
            text: "Pregunta 2",
            assignedScore: 200,
            timeLimitSeconds: 45,
            displayOrder: 2,
            options: expect.arrayContaining([
              expect.objectContaining({ text: "C2", isCorrect: true })
            ])
          })
        ]
      }),
      "operator-token"
    );
    expect(await screen.findByRole("status")).toHaveTextContent("2 preguntas");
  });

  it("loads lobby participants for HU-22", async () => {
    vi.spyOn(triviaApi, "getOperatorTriviaGamesForSupervision").mockResolvedValue([
      createSupervisableGame({ id: "game-1", nombre: "Trivia demo", estado: "Lobby" })
    ]);
    vi.spyOn(triviaApi, "getTriviaParticipants").mockResolvedValue({
      partidaId: "game-1",
      nombre: "Trivia demo",
      estado: "Lobby",
      modalidad: "Individual",
      tiempoInicio: "2026-01-01T00:00:00Z",
      minimoParticipantes: 1,
      maximoJugadores: 10,
      participantesActual: 1,
      participantes: [{ inscripcionId: "i1", usuarioId: "u1", fechaInscripcion: "2026-01-01T00:00:00Z" }]
    });
    vi.spyOn(triviaApi, "getTriviaTeams").mockResolvedValue([]);
    vi.spyOn(triviaApi, "getTriviaRanking").mockResolvedValue([]);

    render(<TriviaOperationsPage accessToken="operator-token" />);

    expect(screen.queryByLabelText(/id de partida trivia/i)).not.toBeInTheDocument();
    await userEvent.selectOptions(await screen.findByLabelText(/partida trivia/i), "game-1");

    expect(await screen.findByText("u1")).toBeInTheDocument();
    expect(screen.getByText(/todavia no hay posiciones de ranking/i)).toBeInTheDocument();
  });

  it("starts a Trivia game for HU-24", async () => {
    vi.spyOn(triviaApi, "getOperatorTriviaGamesForSupervision").mockResolvedValue([
      createSupervisableGame({ id: "game-1", nombre: "Trivia demo", estado: "Lobby" })
    ]);
    vi.spyOn(triviaApi, "getTriviaParticipants").mockResolvedValue({
      partidaId: "game-1",
      nombre: "Trivia demo",
      estado: "Lobby",
      modalidad: "Individual",
      tiempoInicio: "2026-01-01T00:00:00Z",
      minimoParticipantes: 1,
      maximoJugadores: 10,
      participantesActual: 1,
      participantes: [{ inscripcionId: "i1", usuarioId: "u1", fechaInscripcion: "2026-01-01T00:00:00Z" }]
    });
    vi.spyOn(triviaApi, "getTriviaTeams").mockResolvedValue([]);
    vi.spyOn(triviaApi, "getTriviaRanking").mockResolvedValue([]);
    const startSpy = vi.spyOn(triviaApi, "startTriviaGame").mockResolvedValue({
      id: "game-1",
      nombre: "Trivia demo",
      estado: "Iniciada",
      modalidad: "Individual",
      modoInicio: "Manual",
      formularioId: "form-1",
      tiempoInicio: "2026-01-01T00:00:00Z",
      minimoParticipantes: 1,
      maximoJugadores: 10,
      maximoEquipos: null,
      minimoJugadoresPorEquipo: null,
      maximoJugadoresPorEquipo: null,
      createdAtUtc: "2026-01-01T00:00:00Z",
      startedAtUtc: "2026-01-01T00:01:00Z"
    });

    render(<TriviaOperationsPage accessToken="operator-token" />);

    await userEvent.selectOptions(await screen.findByLabelText(/partida trivia/i), "game-1");
    await userEvent.click(screen.getByRole("button", { name: /iniciar trivia/i }));

    expect(startSpy).toHaveBeenCalledWith("game-1", "operator-token");
    expect(await screen.findByRole("status")).toHaveTextContent("Partida iniciada");
  });

  it("loads Trivia ranking for HU-30", async () => {
    vi.spyOn(triviaApi, "getOperatorTriviaGamesForSupervision").mockResolvedValue([
      createSupervisableGame({ id: "game-1", nombre: "Trivia demo", estado: "Iniciada" })
    ]);
    vi.spyOn(triviaApi, "getTriviaParticipants").mockResolvedValue({
      partidaId: "game-1",
      nombre: "Trivia demo",
      estado: "Iniciada",
      modalidad: "Individual",
      tiempoInicio: "2026-01-01T00:00:00Z",
      minimoParticipantes: 1,
      maximoJugadores: 10,
      participantesActual: 1,
      participantes: []
    });
    vi.spyOn(triviaApi, "getTriviaTeams").mockResolvedValue([]);
    vi.spyOn(triviaApi, "getTriviaRanking").mockResolvedValue([
      { usuarioId: "u1", puntajeAcumulado: 300, tiempoAcumuladoSegundos: 12, respuestasCorrectas: 3, totalRespuestas: 3, posicion: 1 }
    ]);

    render(<TriviaOperationsPage accessToken="operator-token" />);

    await userEvent.selectOptions(await screen.findByLabelText(/partida trivia/i), "game-1");

    expect(await screen.findByText("300")).toBeInTheDocument();
  });

  it("disables start action for already started Trivia games", async () => {
    vi.spyOn(triviaApi, "getOperatorTriviaGamesForSupervision").mockResolvedValue([
      createSupervisableGame({ id: "game-1", nombre: "Trivia iniciada", estado: "Iniciada" })
    ]);
    vi.spyOn(triviaApi, "getTriviaParticipants").mockResolvedValue({
      partidaId: "game-1",
      nombre: "Trivia iniciada",
      estado: "Iniciada",
      modalidad: "Individual",
      tiempoInicio: "2026-01-01T00:00:00Z",
      minimoParticipantes: 1,
      maximoJugadores: 10,
      participantesActual: 0,
      participantes: []
    });
    vi.spyOn(triviaApi, "getTriviaTeams").mockResolvedValue([]);
    vi.spyOn(triviaApi, "getTriviaRanking").mockResolvedValue([]);

    render(<TriviaOperationsPage accessToken="operator-token" />);

    await userEvent.selectOptions(await screen.findByLabelText(/partida trivia/i), "game-1");

    expect(await screen.findByText(/Estado Iniciada/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /iniciar trivia/i })).toBeDisabled();
  });

  it("renders empty state when there are no supervisable Trivia games", async () => {
    render(<TriviaOperationsPage accessToken="operator-token" />);

    expect(await screen.findByText(/no hay partidas trivia en lobby o iniciadas/i)).toBeInTheDocument();
  });
});

function createSupervisableGame(patch: Partial<triviaApi.TriviaGameListItem>): triviaApi.TriviaGameListItem {
  return {
    id: "game-1",
    nombre: "Trivia demo",
    modalidad: "Individual",
    estado: "Lobby",
    tiempoInicio: "2026-01-01T00:00:00Z",
    minimoParticipantes: 1,
    maximoJugadores: 10,
    maximoEquipos: null,
    ...patch
  };
}
