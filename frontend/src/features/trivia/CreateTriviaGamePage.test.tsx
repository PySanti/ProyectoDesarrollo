import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { CreateTriviaGamePage } from "./CreateTriviaGamePage";
import * as triviaApi from "../../api/triviaApi";

describe("CreateTriviaGamePage", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("renders HU-17 Trivia creation form", () => {
    vi.spyOn(triviaApi, "getTriviaForms").mockResolvedValue([
      { id: "f1", title: "Formulario A", isComplete: true, questionsCount: 5, createdAtUtc: "2026-01-01T00:00:00Z" }
    ]);

    render(<CreateTriviaGamePage accessToken="token" />);

    expect(screen.getByRole("heading", { name: /crear partida de trivia/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/nombre/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/formulario/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/modalidad/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/modo de inicio/i)).toBeInTheDocument();
  });

  it("shows forms dropdown with fetched trivia forms", async () => {
    vi.spyOn(triviaApi, "getTriviaForms").mockResolvedValue([
      { id: "f1", title: "Formulario A", isComplete: true, questionsCount: 5, createdAtUtc: "2026-01-01T00:00:00Z" },
      { id: "f2", title: "Formulario B", isComplete: true, questionsCount: 3, createdAtUtc: "2026-01-01T00:00:00Z" }
    ]);

    render(<CreateTriviaGamePage accessToken="token" />);

    expect(await screen.findByText("Formulario A (5 preguntas)")).toBeInTheDocument();
    expect(screen.getByText("Formulario B (3 preguntas)")).toBeInTheDocument();
  });

  it("shows disabled dropdown when no complete forms exist", async () => {
    vi.spyOn(triviaApi, "getTriviaForms").mockResolvedValue([
      { id: "f1", title: "Incompleto", isComplete: false, questionsCount: 0, createdAtUtc: "2026-01-01T00:00:00Z" }
    ]);

    render(<CreateTriviaGamePage accessToken="token" />);

    expect(await screen.findByText("No hay formularios completos disponibles")).toBeInTheDocument();
  });

  it("submits individual Trivia creation and shows success", async () => {
    vi.spyOn(triviaApi, "getTriviaForms").mockResolvedValue([
      { id: "f1", title: "Formulario A", isComplete: true, questionsCount: 5, createdAtUtc: "2026-01-01T00:00:00Z" }
    ]);

    const spy = vi.spyOn(triviaApi, "createTriviaGame").mockResolvedValue({
      id: "g1",
      nombre: "Trivia Semanal",
      estado: "Lobby",
      modalidad: "Individual",
      modoInicio: "Manual",
      formularioId: "f1",
      tiempoInicio: "2026-12-31T23:59:59Z",
      minimoParticipantes: 2,
      maximoJugadores: 20,
      maximoEquipos: null,
      minimoJugadoresPorEquipo: null,
      maximoJugadoresPorEquipo: null,
      createdAtUtc: "2026-06-01T00:00:00Z",
      startedAtUtc: null
    });

    render(<CreateTriviaGamePage accessToken="operator-token" />);

    await screen.findByText("Formulario A (5 preguntas)");

    await userEvent.type(screen.getByLabelText(/nombre/i), "Trivia Semanal");
    await userEvent.clear(screen.getByLabelText(/minimo participantes/i));
    await userEvent.type(screen.getByLabelText(/minimo participantes/i), "2");
    await userEvent.clear(screen.getByLabelText(/maximo jugadores/i));
    await userEvent.type(screen.getByLabelText(/maximo jugadores/i), "20");
    await userEvent.click(screen.getByRole("button", { name: /crear partida de trivia/i }));

    expect(spy).toHaveBeenCalledWith(
      expect.objectContaining({
        nombre: "Trivia Semanal",
        modalidad: "Individual",
        minimoParticipantes: 2,
        maximoJugadores: 20,
        maximoEquipos: null
      }),
      "operator-token"
    );
    expect(await screen.findByTestId("trivia-create-success")).toBeInTheDocument();
  });

  it("shows validation error when name is missing", async () => {
    vi.spyOn(triviaApi, "getTriviaForms").mockResolvedValue([
      { id: "f1", title: "Formulario A", isComplete: true, questionsCount: 5, createdAtUtc: "2026-01-01T00:00:00Z" }
    ]);

    render(<CreateTriviaGamePage accessToken="token" />);

    await screen.findByText("Formulario A (5 preguntas)");
    await userEvent.click(screen.getByRole("button", { name: /crear partida de trivia/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent("El nombre de la partida es obligatorio.");
  });

  it("maps 403 to unauthorized message", async () => {
    vi.spyOn(triviaApi, "getTriviaForms").mockResolvedValue([
      { id: "f1", title: "Formulario A", isComplete: true, questionsCount: 5, createdAtUtc: "2026-01-01T00:00:00Z" }
    ]);

    vi.spyOn(triviaApi, "createTriviaGame").mockRejectedValue(
      new triviaApi.TriviaApiError("forbidden", 403)
    );

    render(<CreateTriviaGamePage accessToken="participant-token" />);

    await screen.findByText("Formulario A (5 preguntas)");
    await userEvent.type(screen.getByLabelText(/nombre/i), "Trivia Semanal");
    await userEvent.clear(screen.getByLabelText(/minimo participantes/i));
    await userEvent.type(screen.getByLabelText(/minimo participantes/i), "2");
    await userEvent.click(screen.getByRole("button", { name: /crear partida de trivia/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent("No autorizado. Debes tener rol Operador.");
  });

  it("maps 404 to form not found message", async () => {
    vi.spyOn(triviaApi, "getTriviaForms").mockResolvedValue([
      { id: "f1", title: "Formulario A", isComplete: true, questionsCount: 5, createdAtUtc: "2026-01-01T00:00:00Z" }
    ]);

    vi.spyOn(triviaApi, "createTriviaGame").mockRejectedValue(
      new triviaApi.TriviaApiError("not found", 404)
    );

    render(<CreateTriviaGamePage accessToken="operator-token" />);

    await screen.findByText("Formulario A (5 preguntas)");
    await userEvent.type(screen.getByLabelText(/nombre/i), "Trivia Semanal");
    await userEvent.clear(screen.getByLabelText(/minimo participantes/i));
    await userEvent.type(screen.getByLabelText(/minimo participantes/i), "2");
    await userEvent.click(screen.getByRole("button", { name: /crear partida de trivia/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent("El formulario seleccionado no existe.");
  });
});
