export interface TriviaOptionRequest {
  text: string;
  isCorrect: boolean;
}

export interface TriviaQuestionRequest {
  text: string;
  assignedScore: number;
  timeLimitSeconds: number;
  displayOrder: number;
  options: TriviaOptionRequest[];
}

export interface CreateTriviaFormRequest {
  title: string;
  questions: TriviaQuestionRequest[];
}

export interface TriviaQuestionResponse {
  id: string;
  text: string;
  assignedScore: number;
  timeLimitSeconds: number;
  displayOrder: number;
  options: { index: number; text: string; isCorrect: boolean }[];
}

export interface CreateTriviaFormResponse {
  id: string;
  title: string;
  isComplete: boolean;
  incompleteReasons: string[];
  createdAtUtc: string;
  updatedAtUtc: string;
  questions: TriviaQuestionResponse[];
}

export class TriviaApiError extends Error {
  constructor(
    message: string,
    public readonly statusCode: number
  ) {
    super(message);
    this.name = "TriviaApiError";
  }
}

const baseUrl = import.meta.env.VITE_TRIVIA_API_BASE_URL as string | undefined;

function resolveBaseUrl(): string {
  if (!baseUrl) {
    throw new Error("Missing VITE_TRIVIA_API_BASE_URL environment variable.");
  }
  return baseUrl.replace(/\/$/, "");
}

export async function createTriviaForm(
  payload: CreateTriviaFormRequest,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<CreateTriviaFormResponse> {
  const response = await fetchImpl(`${resolveBaseUrl()}/api/trivia-forms`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${accessToken}`
    },
    body: JSON.stringify(payload)
  });

  const body = (await response.json().catch(() => ({}))) as
    | { message?: string }
    | CreateTriviaFormResponse;

  if (!response.ok) {
    const message =
      (body as { message?: string }).message ??
      `Trivia API error. Status=${response.status}`;
    throw new TriviaApiError(message, response.status);
  }

  return body as CreateTriviaFormResponse;
}
