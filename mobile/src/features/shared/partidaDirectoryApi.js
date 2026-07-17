// Directorio de nombres de partida. Espejo de directoryApi.js (que resuelve competidores
// contra Identity), pero contra Operaciones de Sesion: el nombre vive alli como snapshot
// (SesionPartida.Nombre) y el gateway le cierra /partidas/** al Participante.
import { mapCommonError, networkError } from "../partidas/partidasPublicadasApi.js";

export async function resolverNombresPartida(apiBaseUrl, token, payload, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/operaciones-sesion/directory/partidas`, {
      method: "POST",
      headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
      body: JSON.stringify(payload),
    });
  } catch {
    return networkError();
  }
  const body = await response.json().catch(() => null);
  if (!response.ok) {
    return mapCommonError(response.status, body);
  }
  return { ok: true, data: { partidas: body?.partidas ?? [] } };
}
