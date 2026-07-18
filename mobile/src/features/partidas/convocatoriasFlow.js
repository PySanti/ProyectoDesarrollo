import { getMisConvocatorias, aceptarConvocatoria, rechazarConvocatoria } from "./convocatoriasApi.js";

export function fetchConvocatorias({ apiBaseUrl, token, fetchImpl }) {
  return getMisConvocatorias(apiBaseUrl, token, fetchImpl ?? fetch);
}

export function responderConvocatoria({ apiBaseUrl, token, convocatoriaId, aceptar, fetchImpl }) {
  const f = fetchImpl ?? fetch;
  return aceptar
    ? aceptarConvocatoria(apiBaseUrl, token, convocatoriaId, f)
    : rechazarConvocatoria(apiBaseUrl, token, convocatoriaId, f);
}

// Tras aceptar, el miembro debe ir al lobby de la partida (donde el lider ya espera): ahi se
// suscribe al grupo SignalR y transiciona solo al iniciarse. Sin esto se queda en la pantalla
// de convocatorias, nunca entra al grupo y su panel no cambia. Rechazar no navega.
export function destinoTrasResponder(convocatoria, aceptar) {
  return aceptar ? { partidaId: convocatoria.partidaId, nombre: convocatoria.nombrePartida } : null;
}
