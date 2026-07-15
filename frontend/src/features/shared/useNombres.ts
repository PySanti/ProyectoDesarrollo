// Resolución de nombres de competidores con caché incremental a nivel de módulo.
//
// La caché es requisito funcional, no optimización: en la sesión en vivo llegan
// competidores nuevos por push de SignalR, así que el hook debe pedir solo los ids
// que aún no conoce cada vez que la lista crece.
//
// Contrato con las pantallas: nombreDe(id) SIEMPRE devuelve algo pintable. Si el
// directorio falla o el id no existe, cae al GUID corto. Ninguna pantalla maneja
// el error, porque la resolución de nombres nunca puede romper la operación.
import { useEffect, useState } from "react";
import { resolverNombres, type ResolverNombresPayload } from "../../api/directoryApi";

const MAX_LOTE = 200;

// null = id ya pedido y no resuelto (usuario dado de baja, equipo eliminado).
// Se cachea para no repedirlo en bucle.
const cache = new Map<string, string | null>();

export function resetNombresCache(): void {
  cache.clear();
}

export function nombreCorto(id: string): string {
  return id.slice(0, 8);
}

// Reparte las entradas de un ranking en las dos listas que pide useNombres. Los DTOs de
// Puntuaciones ya traen tipoCompetidor, así que se reparte por él y no se adivina.
export function idsDeCompetidores(
  entradas: readonly { competidorId: string; tipoCompetidor: string }[]
): { participanteIds: string[]; equipoIds: string[] } {
  return {
    participanteIds: entradas.filter((e) => e.tipoCompetidor === "Participante").map((e) => e.competidorId),
    equipoIds: entradas.filter((e) => e.tipoCompetidor === "Equipo").map((e) => e.competidorId)
  };
}

function trocear(participanteIds: string[], equipoIds: string[]): ResolverNombresPayload[] {
  const lotes: ResolverNombresPayload[] = [];
  let p = 0;
  let e = 0;

  while (p < participanteIds.length || e < equipoIds.length) {
    const loteP = participanteIds.slice(p, p + MAX_LOTE);
    const loteE = equipoIds.slice(e, e + (MAX_LOTE - loteP.length));
    lotes.push({ participanteIds: loteP, equipoIds: loteE });
    p += loteP.length;
    e += loteE.length;
  }

  return lotes;
}

export function useNombres(
  ids: { participanteIds: string[]; equipoIds: string[] },
  accessToken: string
): (id: string) => string {
  const [, setVersion] = useState(0);
  const claveParticipantes = ids.participanteIds.join(",");
  const claveEquipos = ids.equipoIds.join(",");

  useEffect(() => {
    let activo = true;
    const faltanP = ids.participanteIds.filter((id) => !cache.has(id));
    const faltanE = ids.equipoIds.filter((id) => !cache.has(id));
    if (faltanP.length === 0 && faltanE.length === 0) return;

    void (async () => {
      for (const lote of trocear(faltanP, faltanE)) {
        try {
          const respuesta = await resolverNombres(lote, accessToken);
          for (const p of respuesta.participantes) cache.set(p.participanteId, p.nombre);
          for (const e of respuesta.equipos) cache.set(e.equipoId, e.nombreEquipo);
          // Lo pedido que no volvió no existe: se marca para no repedirlo.
          for (const id of [...lote.participanteIds, ...lote.equipoIds]) {
            if (!cache.has(id)) cache.set(id, null);
          }
        } catch {
          // Degradación deliberada: la pantalla se queda con GUIDs cortos y sigue operativa.
          return;
        }
      }
      if (activo) setVersion((v) => v + 1);
    })();

    return () => {
      activo = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [claveParticipantes, claveEquipos, accessToken]);

  return (id: string) => cache.get(id) ?? nombreCorto(id);
}
