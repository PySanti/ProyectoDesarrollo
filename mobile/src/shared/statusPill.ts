import { PillState } from './ui';

/**
 * Mapea el `estado` de una partida (string del backend) a un chip de estado de marca,
 * conservando el texto original como etiqueta (no rompe nada que dependa de ese texto).
 * Reutilizado por Trivia y BDT. Estado = color + texto + forma.
 */
export function gameStatePill(estado: string): { state: PillState; label: string } {
  const e = (estado ?? '').toLowerCase();
  if (e.includes('iniciad') || e.includes('curso') || e.includes('vivo') || e.includes('progreso')) {
    return { state: 'live', label: estado };
  }
  if (e.includes('cancel')) {
    return { state: 'cancel', label: estado };
  }
  if (e.includes('termin') || e.includes('finaliz') || e.includes('cerrad')) {
    return { state: 'done', label: estado };
  }
  // lobby / publicada / en espera / programada / abierta → estado calmo de espera
  return { state: 'lobby', label: estado };
}
