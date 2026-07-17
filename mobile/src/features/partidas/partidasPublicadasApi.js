// Errores comunes de los endpoints de participacion (Bloque 2d).
export function mapCommonError(status, body) {
  if (status === 401) {
    return { ok: false, type: "unauthorized", message: "Sesion expirada o no autorizada." };
  }
  if (status === 403) {
    return { ok: false, type: "forbidden", message: body?.message || "No tienes permiso para esta accion." };
  }
  if (status === 404) {
    return { ok: false, type: "not_found", message: body?.message || "La partida no existe o no esta publicada." };
  }
  if (status === 409) {
    return { ok: false, type: "conflict", message: body?.message || "La accion entra en conflicto con el estado actual." };
  }
  return { ok: false, type: "error", message: body?.message || "Ocurrio un error inesperado." };
}

export const networkError = () => ({
  ok: false,
  type: "network",
  message: "No se pudo conectar con el servidor. Verifica tu conexion e intenta de nuevo.",
});

export async function getPartidasPublicadas(apiBaseUrl, token, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/operaciones-sesion/partidas-publicadas`, {
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
  return { ok: true, data: body ?? [] };
}
