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
