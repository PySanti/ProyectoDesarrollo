import { createTeam } from "./createTeamApi.js";
import { nombreEquipo } from "../../shared/validation.js";

export async function submitCreateTeam({ apiBaseUrl, token, teamName, fetchImpl = fetch }) {
  const validationError = nombreEquipo(teamName);
  if (validationError) {
    return { ok: false, type: "validation", message: validationError };
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
