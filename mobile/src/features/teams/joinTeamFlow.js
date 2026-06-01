import { joinTeamByCode } from "./joinTeamApi.js";

export async function submitJoinTeamByCode({ apiBaseUrl, token, accessCode, fetchImpl }) {
  const normalizedCode = accessCode?.trim().toUpperCase();

  if (!normalizedCode) {
    return { ok: false, type: "validation", message: "El codigo de acceso es obligatorio." };
  }

  let result;
  try {
    result = await joinTeamByCode(
      apiBaseUrl,
      token,
      { codigoAcceso: normalizedCode },
      fetchImpl,
    );
  } catch {
    return {
      ok: false,
      type: "error",
      message: "Ocurrio un error inesperado al unirte al equipo. Intenta nuevamente.",
    };
  }

  if (!result.ok && result.type === "conflict") {
    return {
      ok: false,
      type: "conflict",
      message: "Ya perteneces a un equipo activo o el equipo destino esta lleno.",
    };
  }

  return result;
}
