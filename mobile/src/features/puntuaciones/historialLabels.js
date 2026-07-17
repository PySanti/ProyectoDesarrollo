// Etiquetas del historial, puras y testeables sin render (mismo patron que liveLabels.js
// en movil y juegoLabels.ts en web). El .tsx no se puede importar desde node --test, asi
// que la logica que tenga casos borde vive aqui.

// Lo unico que las dos lineas comparten de verdad: unir omitiendo lo ausente, sin dejar
// separadores sueltos. La composicion de cada una queda explicita en su funcion.
const unir = (partes) => partes.filter(Boolean).join(" · ");

const fechaCorta = (fechaFin) => (fechaFin ? new Date(fechaFin).toLocaleDateString() : null);

// Linea de contexto de una partida jugada: modalidad, puntos, posicion y fecha.
// Modalidad y fecha son nullable en el DTO; se omiten sin dejar separadores sueltos.
// El JSDoc no es adorno: sin el, TS infiere los campos como requeridos y el .tsx no
// puede pasar su PartidaHistorial, que los tiene opcionales.
/**
 * @param {{
 *   modalidad?: string | null,
 *   puntosTotales: number,
 *   posicion: number,
 *   fechaFin?: string | null
 * }} partida
 * @returns {string}
 */
export function lineaContextoPartida({ modalidad, puntosTotales, posicion, fechaFin }) {
  return unir([modalidad, `${puntosTotales} pts`, `Posición ${posicion}`, fechaCorta(fechaFin)]);
}

// Rendimiento de equipo: su DTO no trae modalidad ni puntos (la partida es por equipos
// por definicion, y el endpoint solo proyecta posicion y victoria). Por eso es una linea
// propia y no una reutilizacion de lineaContextoPartida, que daria "undefined pts".
/**
 * @param {{ posicion: number, fechaFin?: string | null }} partida
 * @returns {string}
 */
export function lineaContextoRendimiento({ posicion, fechaFin }) {
  return unir([`Posición ${posicion}`, fechaCorta(fechaFin)]);
}
