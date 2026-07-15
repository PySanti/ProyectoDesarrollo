import { getPartidasPublicadas } from "./partidasPublicadasApi.js";
import { getMiSesion } from "./miSesionApi.js";

export async function cargarPanel({ apiBaseUrl, token, fetchImpl }) {
  const [listado, miSesion] = await Promise.all([
    getPartidasPublicadas(apiBaseUrl, token, fetchImpl ?? fetch),
    getMiSesion(apiBaseUrl, token, fetchImpl ?? fetch),
  ]);
  if (!listado.ok) {
    return { ok: false, message: listado.message };
  }
  // mi-sesion caida no bloquea el panel: banner simplemente no aparece.
  return { ok: true, partidas: listado.data, miSesion: miSesion.ok ? miSesion.sesion : null };
}

export function filtrarPorModalidad(partidas, filtro) {
  if (filtro === "Todas") {
    return partidas;
  }
  return partidas.filter((p) => p.modalidad === filtro);
}
