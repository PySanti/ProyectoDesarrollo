// Tipos de evento del historial y su etiqueta para el operador.
//
// La lista es el espejo exacto de lo que el backend proyecta (HistorialEventMapper, 22 tipos):
// si aqui falta uno, ese evento llega al historial pero el operador no puede aislarlo; si sobra,
// el filtro devuelve siempre vacio. Mantener ambas listas alineadas al tocar el contrato.
export const TIPOS_EVENTO = [
  "PartidaPublicadaEnLobby",
  "PartidaIniciada",
  "PartidaCancelada",
  "PartidaFinalizada",
  "JuegoActivado",
  "PreguntaTriviaActivada",
  "RespuestaTriviaValidada",
  "PuntajeTriviaIncrementado",
  "PreguntaTriviaCerrada",
  "EtapaBDTActivada",
  "TesoroQRValidado",
  "EtapaBDTGanada",
  "EtapaBDTCerrada",
  "PistaEnviada",
  "InscripcionSolicitada",
  "InscripcionAceptada",
  "InscripcionRechazada",
  "InscripcionEquipoCreada",
  "InscripcionEquipoCancelada",
  "ConvocatoriaCreada",
  "ConvocatoriaRespondida",
  "UbicacionActualizada"
];

// Etiquetas explicitas y no un humanizador generico: los acronimos del dominio (BDT, QR) se
// parten solos al separar por mayusculas ("Etapa BDTGanada"), y Trivia/BDT se conservan porque
// son lenguaje ubicuo, no ruido.
const ETIQUETAS: Record<string, string> = {
  PartidaPublicadaEnLobby: "Partida publicada en lobby",
  PartidaIniciada: "Partida iniciada",
  PartidaCancelada: "Partida cancelada",
  PartidaFinalizada: "Partida finalizada",
  JuegoActivado: "Juego activado",
  PreguntaTriviaActivada: "Pregunta Trivia activada",
  RespuestaTriviaValidada: "Respuesta Trivia validada",
  PuntajeTriviaIncrementado: "Puntaje Trivia incrementado",
  PreguntaTriviaCerrada: "Pregunta Trivia cerrada",
  EtapaBDTActivada: "Etapa BDT activada",
  TesoroQRValidado: "Tesoro QR validado",
  EtapaBDTGanada: "Etapa BDT ganada",
  EtapaBDTCerrada: "Etapa BDT cerrada",
  PistaEnviada: "Pista enviada",
  InscripcionSolicitada: "Inscripción solicitada",
  InscripcionAceptada: "Inscripción aceptada",
  InscripcionRechazada: "Inscripción rechazada",
  InscripcionEquipoCreada: "Preinscripción de equipo creada",
  InscripcionEquipoCancelada: "Preinscripción de equipo cancelada",
  ConvocatoriaCreada: "Convocatoria creada",
  ConvocatoriaRespondida: "Convocatoria respondida",
  UbicacionActualizada: "Ubicación actualizada"
};

export function etiquetaTipoEvento(tipo: string): string {
  const conocido = ETIQUETAS[tipo];
  if (conocido) return conocido;
  // Un tipo nuevo del backend se muestra humanizado antes que en crudo o en blanco.
  const separado = tipo.replace(/([a-z0-9])([A-Z])/g, "$1 $2").toLowerCase();
  return separado.charAt(0).toUpperCase() + separado.slice(1);
}
