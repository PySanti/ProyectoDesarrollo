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

// Ranking consolidado: orden real = juegosGanados DESC → puntosTotales DESC →
// tiempoTotalMs ASC. El panel muestra juegos (🏆) y puntos, pero NO el tiempo. El
// único desempate que el usuario no puede ver es el del TIEMPO: cuando varios empatan
// en juegos Y puntos y decide el menor tiempo total. Un empate resuelto por juegos o
// por puntos ya se ve en sus columnas.
//
// Agrupa entradas consecutivas empatadas en lo visible (juegos + puntos). Si un grupo
// tiene ≥2 miembros y NO todos tienen el mismo tiempo, el tiempo decidió el orden: se
// marca SOLO el primero del grupo (el primer lugar del empate). Comparar pares sueltos
// fallaba con empates de 3+: podía marcar el 2do en vez del 1ro, o marcar dos filas.
// Asume las entradas ya ordenadas por posición.
/** @returns {Record<string, string>} motivo del desempate por competidorId */
export function motivosDesempateConsolidado(entradas) {
  const lista = entradas ?? [];
  const motivos = {};
  const mismaCasilla = (a, b) =>
    (a.juegosGanados ?? 0) === (b.juegosGanados ?? 0) && a.puntosTotales === b.puntosTotales;
  let i = 0;
  while (i < lista.length) {
    let j = i + 1;
    while (j < lista.length && mismaCasilla(lista[i], lista[j])) j++;
    const grupo = lista.slice(i, j);
    const t0 = grupo[0].tiempoTotalMs ?? 0;
    const tiempoDesempata = grupo.some((e) => (e.tiempoTotalMs ?? 0) !== t0);
    if (grupo.length >= 2 && tiempoDesempata) {
      motivos[grupo[0].competidorId] = "por menor tiempo";
    }
    i = j;
  }
  return motivos;
}
