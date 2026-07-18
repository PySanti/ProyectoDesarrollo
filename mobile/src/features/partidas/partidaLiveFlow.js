import { getMiSesion } from "./miSesionApi.js";

// Envía la respuesta de trivia y detecta si la pregunta avanzó (PreguntaActivada llegó por
// SignalR) mientras el POST estaba en vuelo. `resetSignal` se bumpea síncrono al recibir el
// avance, así que un cambio del getter entre antes/después del await es la señal fiable de que
// la pregunta ya cambió. avanzo=true ⇒ el llamador NO debe fijar estado local "respondida":
// la nueva pregunta ya está en curso y fijarlo la dejaría clavada en "¡Correcta! / Esperando…".
// `leerResetSignal` debe leer el valor VIVO (via ref), no el capturado en el render.
export async function enviarRespuestaTrivia(responder, leerResetSignal) {
  const resetAntes = leerResetSignal();
  const r = await responder();
  return { avanzo: leerResetSignal() !== resetAntes, r };
}

// Una fase terminal (finalizada/cancelada) no debe recargarse: recargar llamaría a getMiSesion,
// que ya no reporta participación activa y devolvería sin-participacion, borrando el ranking final.
// El refresh de token (RNF-24) re-dispara el efecto de load, y cada teléfono refresca a su hora,
// por eso el ranking se caía "a algunos antes que a otros".
export function debeRecargarLive(faseStatus) {
  return faseStatus !== "finalizada" && faseStatus !== "cancelada";
}

export async function cargarLive({ apiBaseUrl, token, partidaId, fetchImpl }) {
  const r = await getMiSesion(apiBaseUrl, token, fetchImpl ?? fetch);
  if (!r.ok) return r;
  if (r.sesion == null || r.sesion.partidaId !== partidaId) {
    return { ok: true, fase: "sin-participacion" };
  }
  if (r.sesion.estadoPartida === "Lobby") {
    return { ok: true, fase: "lobby" };
  }
  return {
    ok: true,
    fase: "iniciada",
    juegoActivo: r.sesion.juegoActivo ?? null,
    yaRespondio: r.sesion.yaRespondioPreguntaActual === true,
  };
}
