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
  const sesionActual =
    mia.ok && mia.sesion != null && mia.sesion.partidaId === partidaId ? mia.sesion : null;
  const inscrito = sesionActual != null;
  const estadoInscripcion = sesionActual?.inscripcion?.estado ?? null;
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
  return { ok: true, lobby: body, inscrito, estadoInscripcion, esLider };
}

// HU-12: en Equipo, solo el líder preinscribe. Sin participación activa el aviso debe ser
// explícito; con participación ya en curso el copy existente ("el líder gestiona") basta.
export function avisoLiderEquipo(modalidad, esLider, inscrito) {
  if (modalidad !== "Equipo" || esLider) return null;
  return inscrito
    ? "El líder gestiona la preinscripción del equipo."
    : "Solo el líder del equipo puede preinscribir al equipo.";
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
