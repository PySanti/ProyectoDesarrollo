export interface TriviaGameListItem {
  id: string;
  nombre: string;
  modalidad: 'Individual' | 'Equipo';
  estado: string;
  tiempoInicio: string;
  minimoParticipantes: number;
  maximoJugadores: number | null;
  maximoEquipos: number | null;
}
