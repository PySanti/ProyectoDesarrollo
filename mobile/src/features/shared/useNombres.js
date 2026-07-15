// Resolución de nombres de competidores con caché incremental a nivel de módulo.
// Espejo de frontend/src/features/shared/useNombres.ts: misma semántica, adaptada al
// estilo { ok, data } del cliente móvil (que no lanza).
//
// La caché es requisito funcional, no optimización: en la sesión en vivo llegan
// competidores nuevos por push de SignalR, así que el hook debe pedir solo los ids
// que aún no conoce cada vez que la lista crece.
//
// Contrato con las pantallas: nombreDe(id) SIEMPRE devuelve algo pintable. Si el
// directorio falla o el id no existe, cae al GUID corto.
import { useEffect, useState } from "react";
import { resolverNombres } from "./directoryApi.js";

const MAX_LOTE = 200;

// null = id ya pedido y no resuelto (usuario dado de baja, equipo eliminado).
// Se cachea para no repedirlo en bucle.
const cache = new Map();

export function resetNombresCache() {
  cache.clear();
}

export function nombreCorto(id) {
  return id.slice(0, 8);
}

export function trocear(participanteIds, equipoIds) {
  const lotes = [];
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

export function useNombres(ids, apiBaseUrl, token) {
  const [, setVersion] = useState(0);
  const claveParticipantes = ids.participanteIds.join(",");
  const claveEquipos = ids.equipoIds.join(",");

  useEffect(() => {
    let activo = true;
    const faltanP = ids.participanteIds.filter((id) => !cache.has(id));
    const faltanE = ids.equipoIds.filter((id) => !cache.has(id));
    if (faltanP.length === 0 && faltanE.length === 0) return;

    (async () => {
      for (const lote of trocear(faltanP, faltanE)) {
        const r = await resolverNombres(apiBaseUrl, token, lote);
        // El cliente móvil no lanza: un { ok: false } se trata igual que un fallo de red.
        // La pantalla se queda con GUIDs cortos y sigue operativa.
        if (!r.ok) return;
        for (const p of r.data.participantes) cache.set(p.participanteId, p.nombre);
        for (const eq of r.data.equipos) cache.set(eq.equipoId, eq.nombreEquipo);
        // Lo pedido que no volvió no existe: se marca para no repedirlo.
        for (const id of [...lote.participanteIds, ...lote.equipoIds]) {
          if (!cache.has(id)) cache.set(id, null);
        }
      }
      if (activo) setVersion((v) => v + 1);
    })();

    return () => {
      activo = false;
    };
  }, [claveParticipantes, claveEquipos, apiBaseUrl, token]);

  return (id) => cache.get(id) ?? nombreCorto(id);
}
