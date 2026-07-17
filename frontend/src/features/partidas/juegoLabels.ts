// Etiqueta de un juego para el operador. `Juego` no tiene nombre en el dominio: su única
// identidad propia es el orden dentro de la partida y su tipo.
const TIPO_LEGIBLE: Record<string, string> = {
  Trivia: "Trivia",
  BusquedaDelTesoro: "Búsqueda del Tesoro"
};

export function etiquetaJuego(
  orden: number | null | undefined,
  tipoJuego: string | null | undefined,
  juegoId: string | null | undefined
): string {
  if (orden == null) {
    // Dos vacíos distintos: sin juegoId el evento es de partida y no hay juego que
    // nombrar; con juegoId el juego existe pero su proyección falta, y decir "—"
    // ocultaría que hay uno.
    return juegoId ? juegoId.slice(0, 8) : "—";
  }

  const tipo = tipoJuego ? (TIPO_LEGIBLE[tipoJuego] ?? tipoJuego) : "";
  return tipo ? `Juego ${orden} · ${tipo}` : `Juego ${orden}`;
}
