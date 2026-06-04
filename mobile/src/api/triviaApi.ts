import { TriviaGameListItem } from "../features/trivia/types";

export type TriviaModalidadFilter = "Individual" | "Equipo";

export class TriviaMobileApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
  ) {
    super(message);
    this.name = "TriviaMobileApiError";
  }
}

type GetPublishedTriviaGamesParams = {
  apiBaseUrl: string;
  token: string;
  modalidad?: TriviaModalidadFilter;
  fetchImpl?: typeof fetch;
};

export async function getPublishedTriviaGames({
  apiBaseUrl,
  token,
  modalidad,
  fetchImpl = fetch,
}: GetPublishedTriviaGamesParams): Promise<TriviaGameListItem[]> {
  const baseUrl = apiBaseUrl.replace(/\/$/, "");
  const query = modalidad ? `?modalidad=${encodeURIComponent(modalidad)}` : "";
  const response = await fetchImpl(`${baseUrl}/api/trivia-games${query}`, {
    method: "GET",
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });

  const body = (await response.json().catch(() => ({}))) as
    | { message?: string }
    | TriviaGameListItem[];

  if (!response.ok) {
    const message =
      (body as { message?: string }).message ??
      `No se pudieron cargar las partidas de Trivia. Status=${response.status}`;
    throw new TriviaMobileApiError(message, response.status);
  }

  return body as TriviaGameListItem[];
}
