const MESES = [
  "enero",
  "febrero",
  "marzo",
  "abril",
  "mayo",
  "junio",
  "julio",
  "agosto",
  "septiembre",
  "octubre",
  "noviembre",
  "diciembre",
];

/**
 * Formatea la `fechaRegistro` (ISO UTC) del historial de equipos a fecha larga en español.
 * Usa getters UTC para que la medianoche `Z` no corra el día por zona horaria, y un array de meses
 * (no Intl) para dar una salida determinista y testeable en cualquier motor.
 */
export function formatFechaRegistro(iso) {
  if (typeof iso !== "string" || iso.length === 0) return "";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso; // valor raro: no romper, mostrar crudo
  return `${d.getUTCDate()} de ${MESES[d.getUTCMonth()]} de ${d.getUTCFullYear()}`;
}
