// Lógica pura de etiquetado del ranking del participante.
//
// Vive en .js y no dentro de liveShared.tsx porque el harness `node --test` no puede
// importar .tsx. Mismo patrón que DeleteTeamScreenController.js.

export function etiquetaCompetidor(competidorId, resaltarId, nombreDe) {
  return competidorId === resaltarId ? "Tú" : nombreDe(competidorId);
}

// Reparte las entradas de un ranking en las dos listas que pide useNombres.
// Sin tipoCompetidor se asume Participante: es la modalidad Individual, donde el
// competidor siempre es una persona.
export function idsDeCompetidores(entradas) {
  const participanteIds = [];
  const equipoIds = [];
  for (const e of entradas ?? []) {
    if (e.tipoCompetidor === "Equipo") equipoIds.push(e.competidorId);
    else participanteIds.push(e.competidorId);
  }
  return { participanteIds, equipoIds };
}
