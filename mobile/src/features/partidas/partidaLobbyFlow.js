import { mapCommonError, networkError } from "./partidasPublicadasApi.js";
import {
  inscribirse,
  cancelarInscripcion,
  preinscribirEquipo,
  cancelarPreinscripcionEquipo,
} from "./inscripcionApi.js";
import { getMiSesion } from "./miSesionApi.js";
import { parseJwtPayload } from "../../auth/tokenClaims.js";

export async function cargarLobby({ apiBaseUrl, token, partidaId, fetchImpl }) {
  const f = fetchImpl ?? fetch;
  let response;
  try {
    response = await f(`${apiBaseUrl}/operaciones-sesion/partidas/${partidaId}/lobby`, {
      method: "GET",
      headers: { Authorization: `Bearer ${token}` },
    });
  } catch {
    return networkError();
  }
  const body = await response.json().catch(() => null);
  if (!response.ok) {
    return mapCommonError(response.status, body);
  }
  const mia = await getMiSesion(apiBaseUrl, token, f);
  const inscrito = mia.ok && mia.sesion != null && mia.sesion.partidaId === partidaId;
  let esLider = true;
  if (body.modalidad === "Equipo") {
    esLider = false;
    try {
      const tm = await f(`${apiBaseUrl}/identity/teams/mine`, {
        method: "GET",
        headers: { Authorization: `Bearer ${token}` },
      });
      if (tm.ok) {
        const team = await tm.json().catch(() => null);
        const sub = parseJwtPayload(token).sub;
        const yo = team?.participantes?.find((p) => p.usuarioId === sub);
        esLider = yo?.esLider === true;
      }
    } catch {
      // sin red hacia identity: tratar como miembro (el backend protege igual con 403)
    }
  }
  return { ok: true, lobby: body, inscrito, esLider };
}

export function accionParticipacion({ apiBaseUrl, token, partidaId, modalidad, inscrito, fetchImpl }) {
  const f = fetchImpl ?? fetch;
  if (modalidad === "Equipo") {
    return inscrito
      ? cancelarPreinscripcionEquipo(apiBaseUrl, token, partidaId, f)
      : preinscribirEquipo(apiBaseUrl, token, partidaId, f);
  }
  return inscrito
    ? cancelarInscripcion(apiBaseUrl, token, partidaId, f)
    : inscribirse(apiBaseUrl, token, partidaId, f);
}
