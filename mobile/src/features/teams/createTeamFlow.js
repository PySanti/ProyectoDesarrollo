import { createTeam } from "./createTeamApi.js";

export async function submitCreateTeam({ apiBaseUrl, token, teamName, fetchImpl }) {
  if (!teamName || !teamName.trim()) {
    return { ok: false, type: "validation", message: "El nombre del equipo es obligatorio." };
  }

  let result;
  try {
    result = await createTeam(
      apiBaseUrl,
      token,
      { nombreEquipo: teamName.trim() },
      fetchImpl,
    );
  } catch {
    return {
      ok: false,
      type: "error",
      message: "Ocurrio un error inesperado al crear el equipo. Intenta nuevamente.",
    };
  }

  if (!result.ok && result.type === "conflict") {
    return {
      ok: false,
      type: "conflict",
      message: "Ya perteneces a un equipo activo. No puedes crear otro equipo.",
    };
  }

  return result;
}
