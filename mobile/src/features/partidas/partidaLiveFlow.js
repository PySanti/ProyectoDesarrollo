import { getMiSesion } from "./miSesionApi.js";

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
