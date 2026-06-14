import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { TriviaOperationsPage } from "./TriviaOperationsPage";
import * as triviaApi from "../../api/triviaApi";

describe("TriviaOperationsPage (supervisión)", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.spyOn(triviaApi, "getOperatorTriviaGamesForSupervision").mockResolvedValue([]);
  });

  it("renders empty state when there are no supervisable Trivia games", async () => {
    render(<TriviaOperationsPage accessToken="operator-token" />);

    expect(
      await screen.findByText(/no hay partidas trivia en lobby o iniciadas/i)
    ).toBeInTheDocument();
  });

  it("loads lobby participants for HU-22", async () => {
    vi.spyOn(triviaApi, "getOperatorTriviaGamesForSupervision").mockResolvedValue([
      createSupervisableGame({ id: "game-1", nombre: "Trivia demo", estado: "Lobby" })
    ]);
    vi.spyOn(triviaApi, "getTriviaParticipants").mockResolvedValue(
      createLobby({
        participantes: [{ inscripcionId: "i1", usuarioId: "u1", fechaInscripcion: "2026-01-01T00:00:00Z" }]
      })
    );
    vi.spyOn(triviaApi, "getTriviaTeams").mockResolvedValue([]);
    vi.spyOn(triviaApi, "getTriviaRanking").mockResolvedValue([]);

    render(<TriviaOperationsPage accessToken="operator-token" />);

    // No bare <select> anymore: supervision is a master list of partidas.
    expect(screen.queryByLabelText(/partida trivia/i)).not.toBeInTheDocument();
    await userEvent.click(await screen.findByRole("button", { name: /trivia demo/i }));

    expect(await screen.findByText("u1")).toBeInTheDocument();
    expect(screen.getByText(/todavia no hay posiciones de ranking/i)).toBeInTheDocument();
  });

  it("starts a Trivia game for HU-24", async () => {
    vi.spyOn(triviaApi, "getOperatorTriviaGamesForSupervision").mockResolvedValue([
      createSupervisableGame({ id: "game-1", nombre: "Trivia demo", estado: "Lobby" })
    ]);
    vi.spyOn(triviaApi, "getTriviaParticipants").mockResolvedValue(
      createLobby({
        participantes: [{ inscripcionId: "i1", usuarioId: "u1", fechaInscripcion: "2026-01-01T00:00:00Z" }]
      })
    );
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

    await userEvent.click(await screen.findByRole("button", { name: /trivia demo/i }));
    await userEvent.click(screen.getByRole("button", { name: /iniciar trivia/i }));

    expect(startSpy).toHaveBeenCalledWith("game-1", "operator-token");
    expect(await screen.findByRole("status")).toHaveTextContent("Partida iniciada");
  });

  it("loads Trivia ranking for HU-30", async () => {
    vi.spyOn(triviaApi, "getOperatorTriviaGamesForSupervision").mockResolvedValue([
      createSupervisableGame({ id: "game-1", nombre: "Trivia demo", estado: "Iniciada" })
    ]);
    vi.spyOn(triviaApi, "getTriviaParticipants").mockResolvedValue(
      createLobby({ estado: "Iniciada", participantes: [] })
    );
    vi.spyOn(triviaApi, "getTriviaTeams").mockResolvedValue([]);
    vi.spyOn(triviaApi, "getTriviaRanking").mockResolvedValue([
      {
        usuarioId: "u1",
        puntajeAcumulado: 300,
        tiempoAcumuladoSegundos: 12,
        respuestasCorrectas: 3,
        totalRespuestas: 3,
        posicion: 1
      }
    ]);

    render(<TriviaOperationsPage accessToken="operator-token" />);

    await userEvent.click(await screen.findByRole("button", { name: /trivia demo/i }));

    expect(await screen.findByText("300")).toBeInTheDocument();
  });

  it("disables start action for already started Trivia games", async () => {
    vi.spyOn(triviaApi, "getOperatorTriviaGamesForSupervision").mockResolvedValue([
      createSupervisableGame({ id: "game-1", nombre: "Trivia iniciada", estado: "Iniciada" })
    ]);
    vi.spyOn(triviaApi, "getTriviaParticipants").mockResolvedValue(
      createLobby({ nombre: "Trivia iniciada", estado: "Iniciada", participantes: [] })
    );
    vi.spyOn(triviaApi, "getTriviaTeams").mockResolvedValue([]);
    vi.spyOn(triviaApi, "getTriviaRanking").mockResolvedValue([]);

    render(<TriviaOperationsPage accessToken="operator-token" />);

    await userEvent.click(await screen.findByRole("button", { name: /trivia iniciada/i }));

    expect(await screen.findByRole("heading", { name: /trivia iniciada/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /iniciar trivia/i })).toBeDisabled();
  });
});

function createSupervisableGame(
  patch: Partial<triviaApi.TriviaGameListItem>
): triviaApi.TriviaGameListItem {
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

function createLobby(patch: Partial<triviaApi.TriviaGameLobby>): triviaApi.TriviaGameLobby {
  return {
    partidaId: "game-1",
    nombre: "Trivia demo",
    estado: "Lobby",
    modalidad: "Individual",
    tiempoInicio: "2026-01-01T00:00:00Z",
    minimoParticipantes: 1,
    maximoJugadores: 10,
    participantesActual: patch.participantes ? patch.participantes.length : 0,
    participantes: [],
    ...patch
  };
}
