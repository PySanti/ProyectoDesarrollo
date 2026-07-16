import { loadMyTeam } from "./teamPanelApi.js";

export async function fetchMyTeamStatus({ apiBaseUrl, token, currentUserId, fetchImpl }) {
  let result;
  try {
    result = await loadMyTeam(apiBaseUrl, token, fetchImpl);
  } catch {
    return {
      ok: false,
      type: "error",
      message: "Ocurrio un error inesperado al cargar tu equipo. Intenta nuevamente.",
    };
  }

  if (!result.ok) {
    return result;
  }

  if (result.data === null) {
    return { ok: true, status: "sinEquipo" };
  }

  const equipo = result.data;
  const yo = equipo.participantes.find((p) => p.usuarioId === currentUserId);
  const soyLider = yo ? yo.esLider : false;

  return {
    ok: true,
    status: soyLider ? "lider" : "miembro",
    equipoId: equipo.equipoId,
    nombreEquipo: equipo.nombreEquipo,
    participantes: equipo.participantes,
  };
}
