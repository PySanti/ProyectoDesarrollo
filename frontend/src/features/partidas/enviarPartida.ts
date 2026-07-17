// Envio encadenado del wizard de creacion de partida: header -> juegos en orden.
// Reintentable: reusa partidaId y saltos los juegos ya en "ok" para nunca re-postear
// un 201 (evita el 409 por orden duplicado). Detiene la cadena en el primer error.
import {
  addJuegoBdt,
  addJuegoTrivia,
  createPartida,
  PartidasApiError,
  type AddJuegoBdtRequest,
  type AddJuegoTriviaRequest
} from "../../api/partidasApi";
import { buildCreatePartidaRequest, buildJuegoRequest, type CreatePartidaDraft } from "./createPartidaDraft";

export type EstadoEnvio = "pendiente" | "enviando" | "ok" | "error";

export interface EnvioJuego {
  estado: EstadoEnvio;
  mensaje?: string;
}

export interface ResultadoEnvio {
  partidaId: string | null;
  estados: EnvioJuego[];
  completo: boolean;
  errorHeader?: string;
}

function mensajeError(error: unknown): string {
  // Solo PartidasApiError trae mensaje de negocio; fetch lanza TypeError
  // ("Failed to fetch") en fallos de red reales — al usuario, mensaje generico.
  if (error instanceof PartidasApiError) {
    return error.message;
  }
  return "Error de red al enviar la partida.";
}

export async function enviarPartida(
  draft: CreatePartidaDraft,
  accessToken: string,
  previo: { partidaId: string | null; estados: EnvioJuego[] } | null,
  onProgress: (estados: EnvioJuego[], partidaId: string | null) => void,
  deps?: {
    createPartida?: typeof createPartida;
    addJuegoTrivia?: typeof addJuegoTrivia;
    addJuegoBdt?: typeof addJuegoBdt;
  }
): Promise<ResultadoEnvio> {
  const doCreatePartida = deps?.createPartida ?? createPartida;
  const doAddJuegoTrivia = deps?.addJuegoTrivia ?? addJuegoTrivia;
  const doAddJuegoBdt = deps?.addJuegoBdt ?? addJuegoBdt;

  let partidaId = previo?.partidaId ?? null;

  if (!partidaId) {
    try {
      const response = await doCreatePartida(buildCreatePartidaRequest(draft.header), accessToken);
      partidaId = response.partidaId;
    } catch (error) {
      return {
        partidaId: null,
        estados: draft.juegos.map(() => ({ estado: "pendiente" })),
        completo: false,
        errorHeader: mensajeError(error)
      };
    }
  }

  const estados: EnvioJuego[] = draft.juegos.map((_, i) => {
    const previoEstado = previo?.estados[i]?.estado;
    return previoEstado === "ok" ? { estado: "ok" } : { estado: "pendiente" };
  });

  for (let i = 0; i < draft.juegos.length; i++) {
    if (estados[i].estado === "ok") continue;

    estados[i] = { estado: "enviando" };
    onProgress([...estados], partidaId);

    const juego = draft.juegos[i];
    const payload = buildJuegoRequest(juego, i + 1);
    try {
      if (juego.tipo === "Trivia") {
        await doAddJuegoTrivia(partidaId, payload as AddJuegoTriviaRequest, accessToken);
      } else {
        await doAddJuegoBdt(partidaId, payload as AddJuegoBdtRequest, accessToken);
      }
      estados[i] = { estado: "ok" };
      onProgress([...estados], partidaId);
    } catch (error) {
      estados[i] = { estado: "error", mensaje: mensajeError(error) };
      onProgress([...estados], partidaId);
      break;
    }
  }

  return {
    partidaId,
    estados,
    completo: estados.every((e) => e.estado === "ok")
  };
}
