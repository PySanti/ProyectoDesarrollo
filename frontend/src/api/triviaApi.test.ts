import { beforeEach, describe, expect, it, vi } from "vitest";

describe("triviaApi", () => {
  beforeEach(() => {
    vi.resetModules();
    vi.unstubAllEnvs();
  });

  it("calls POST /api/trivia-forms with bearer token and returns created form", async () => {
    vi.stubEnv("VITE_TRIVIA_API_BASE_URL", "https://trivia.example.test/");

    const { createTriviaForm } = await import("./triviaApi");
    const payload = {
      title: "Formulario Test",
      questions: [
        {
          text: "Capital de Francia?",
          assignedScore: 100,
          timeLimitSeconds: 30,
          displayOrder: 1,
          options: [
            { text: "Paris", isCorrect: true },
            { text: "Londres", isCorrect: false },
            { text: "Berlin", isCorrect: false },
            { text: "Madrid", isCorrect: false }
          ]
        }
      ]
    };

    const responseBody = {
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
    };

    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 201,
      json: async () => responseBody
    });

    const result = await createTriviaForm(payload, "operator-token", fetchMock as unknown as typeof fetch);

    expect(fetchMock).toHaveBeenCalledWith(
      "https://trivia.example.test/api/trivia-forms",
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: "Bearer operator-token"
        },
        body: JSON.stringify(payload)
      }
    );
    expect(result.id).toBe("form-1");
    expect(result.title).toBe("Formulario Test");
    expect(result.questions).toHaveLength(1);
  });

  it("throws TriviaApiError on non-OK response", async () => {
    vi.stubEnv("VITE_TRIVIA_API_BASE_URL", "https://trivia.example.test");
    const { createTriviaForm, TriviaApiError } = await import("./triviaApi");

    const payload = {
      title: "Test",
      questions: [
        {
          text: "Q1",
          assignedScore: 100,
          timeLimitSeconds: 30,
          displayOrder: 1,
          options: [
            { text: "A", isCorrect: true },
            { text: "B", isCorrect: false },
            { text: "C", isCorrect: false },
            { text: "D", isCorrect: false }
          ]
        }
      ]
    };

    const fetchMock = vi.fn().mockResolvedValue({
      ok: false,
      status: 403,
      json: async () => ({ message: "Requires Operador role" })
    });

    await expect(
      createTriviaForm(payload, "participant-token", fetchMock as unknown as typeof fetch)
    ).rejects.toMatchObject({
      name: "TriviaApiError",
      message: "Requires Operador role",
      statusCode: 403
    });

    await expect(
      createTriviaForm(payload, "participant-token", fetchMock as unknown as typeof fetch)
    ).rejects.toBeInstanceOf(TriviaApiError);
  });
});
