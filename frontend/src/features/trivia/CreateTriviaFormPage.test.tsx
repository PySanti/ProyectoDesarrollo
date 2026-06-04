import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { CreateTriviaFormPage } from "./CreateTriviaFormPage";
import * as triviaApi from "../../api/triviaApi";

describe("CreateTriviaFormPage", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("renders HU-15 trivia form creation UI", () => {
    render(<CreateTriviaFormPage accessToken="token" />);

    expect(screen.getByRole("heading", { name: /crear formulario de trivia/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/titulo del formulario/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/texto de la pregunta/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /crear formulario/i })).toBeInTheDocument();
  });

  it("submits form and shows success message", async () => {
    const spy = vi.spyOn(triviaApi, "createTriviaForm").mockResolvedValue({
      id: "form-1",
      title: "Formulario Test",
      isComplete: true,
      incompleteReasons: [],
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: "2026-01-01T00:00:00Z",
      questions: [
        {
          id: "q-1",
          text: "Capital de Francia?",
          assignedScore: 100,
          timeLimitSeconds: 30,
          displayOrder: 1,
          options: [
            { index: 0, text: "Paris", isCorrect: true },
            { index: 1, text: "Londres", isCorrect: false },
            { index: 2, text: "Berlin", isCorrect: false },
            { index: 3, text: "Madrid", isCorrect: false }
          ]
        }
      ]
    });

    render(<CreateTriviaFormPage accessToken="token-1" />);

    await userEvent.type(screen.getByLabelText(/titulo del formulario/i), "Formulario Test");
    await userEvent.type(screen.getByLabelText(/texto de la pregunta/i), "Capital de Francia?");
    await userEvent.click(screen.getAllByLabelText(/^Correcta$/i)[0]);

    await userEvent.type(screen.getByLabelText(/^Opcion 1$/i), "Paris");
    const optionInputs = screen.getAllByLabelText(/^Opcion [2-4]$/i);
    await userEvent.type(optionInputs[0], "Londres");
    await userEvent.type(optionInputs[1], "Berlin");
    await userEvent.type(optionInputs[2], "Madrid");

    await userEvent.click(screen.getByRole("button", { name: /crear formulario/i }));

    expect(spy).toHaveBeenCalledWith(
      expect.objectContaining({
        title: "Formulario Test",
        questions: [
          expect.objectContaining({
            text: "Capital de Francia?",
            assignedScore: 100,
            timeLimitSeconds: 30
          })
        ]
      }),
      "token-1"
    );
    expect(await screen.findByTestId("trivia-form-create-success")).toBeInTheDocument();
  });

  it("shows validation error when title is missing", async () => {
    render(<CreateTriviaFormPage accessToken="token" />);

    await userEvent.click(screen.getByRole("button", { name: /crear formulario/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent(/titulo del formulario es obligatorio/i);
  });

  it("shows validation error when no correct option is selected", async () => {
    render(<CreateTriviaFormPage accessToken="token" />);

    await userEvent.type(screen.getByLabelText(/titulo del formulario/i), "Formulario Test");
    await userEvent.type(screen.getByLabelText(/texto de la pregunta/i), "Pregunta valida?");
    const optionInputs = screen.getAllByLabelText(/opcion [1-4]/i);
    await userEvent.type(optionInputs[0], "A");
    await userEvent.type(optionInputs[1], "B");
    await userEvent.type(optionInputs[2], "C");
    await userEvent.type(optionInputs[3], "D");
    await userEvent.click(screen.getByRole("button", { name: /crear formulario/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent(/debe tener exactamente una opcion correcta/i);
  });

  it("maps 403 TriviaApiError to unauthorized message", async () => {
    vi.spyOn(triviaApi, "createTriviaForm").mockRejectedValue(
      new triviaApi.TriviaApiError("Requires Operador role", 403)
    );

    render(<CreateTriviaFormPage accessToken="participant-token" />);

    await userEvent.type(screen.getByLabelText(/titulo del formulario/i), "Test");
    await userEvent.type(screen.getByLabelText(/texto de la pregunta/i), "Q?");
    await userEvent.click(screen.getAllByLabelText(/^Correcta$/i)[0]);
    const optionInputs = screen.getAllByLabelText(/^Opcion [1-4]$/i);
    await userEvent.type(optionInputs[0], "A");
    await userEvent.type(optionInputs[1], "B");
    await userEvent.type(optionInputs[2], "C");
    await userEvent.type(optionInputs[3], "D");
    await userEvent.click(screen.getByRole("button", { name: /crear formulario/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent(/no autorizado/i);
  });

  it("adds and removes questions dynamically", async () => {
    render(<CreateTriviaFormPage accessToken="token" />);

    expect(screen.getAllByRole("group")).toHaveLength(1);

    await userEvent.click(screen.getByRole("button", { name: /agregar pregunta/i }));
    expect(screen.getAllByRole("group")).toHaveLength(2);

    const removeButtons = screen.getAllByRole("button", { name: /eliminar pregunta/i });
    expect(removeButtons).toHaveLength(2);
    await userEvent.click(removeButtons[0]);
    expect(screen.getAllByRole("group")).toHaveLength(1);
  });
});
