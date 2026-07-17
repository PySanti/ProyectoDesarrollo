// Resolucion de nombres de partida con cache a nivel de modulo.
//
// A diferencia del useNombresPartida.ts del web, este SI lleva cache y troceo. No es
// copiar complejidad: el web baja todas las partidas de un GET /partidas y no necesita
// saber que ids pedir; el movil no llega a Partidas (el gateway se lo cierra), asi que
// pide por lote de ids conocidos, y esos ids solo se conocen despues de cargar el
// historial. Ademas tres pantallas comparten el hook (historial, rendimiento, panel).
//
// La logica vive fuera del hook a proposito: node --test no puede renderizar React, y el
// hermano useNombres.js paga ese precio dejando cache y degradacion sin cubrir. Aqui el
// hook es pegamento fino y cargarNombresPartida se prueba directamente.
//
// Contrato con las pantallas: nombrePartidaDe(id) SIEMPRE devuelve algo pintable.
import { useEffect, useState } from "react";
import { resolverNombresPartida } from "./partidaDirectoryApi.js";

const MAX_LOTE = 200;

// null = id ya pedido y no resuelto (partida nunca publicada). Se cachea para no
// repedirlo en bucle.
const cache = new Map();

export function resetNombresPartidaCache() {
  cache.clear();
}

export function nombreCortoPartida(id) {
  return id.slice(0, 8);
}

// Nombre resuelto o null. Para llamadores que quieren elegir su propio fallback en vez
// del GUID corto (p.ej. la cabecera de sesion en vivo, donde "Mi partida" es mejor copy
// que "a3f9c1d2").
export function nombrePartidaResuelto(id) {
  return cache.get(id) ?? null;
}

export function nombrePartidaEnCache(id) {
  return nombrePartidaResuelto(id) ?? nombreCortoPartida(id);
}

export function trocearPartidas(partidaIds) {
  const lotes = [];
  for (let i = 0; i < partidaIds.length; i += MAX_LOTE) {
    lotes.push(partidaIds.slice(i, i + MAX_LOTE));
  }
  return lotes;
}

// Devuelve true si la cache cambio (para que el hook repinte solo entonces).
export async function cargarNombresPartida(partidaIds, apiBaseUrl, token, fetchImpl = fetch) {
  const faltan = partidaIds.filter((id) => !cache.has(id));
  if (faltan.length === 0) return false;

  for (const lote of trocearPartidas(faltan)) {
    const r = await resolverNombresPartida(apiBaseUrl, token, { partidaIds: lote }, fetchImpl);
    // El cliente no lanza: un { ok: false } se trata igual que un fallo de red. Se corta
    // sin cachear nada, para que un reintento posterior pueda resolver.
    if (!r.ok) return false;
    for (const p of r.data.partidas) cache.set(p.partidaId, p.nombre);
    // Lo pedido que no volvio no existe: se marca para no repedirlo.
    for (const id of lote) {
      if (!cache.has(id)) cache.set(id, null);
    }
  }
  return true;
}

export function useNombresPartida(partidaIds, apiBaseUrl, token) {
  const [, setVersion] = useState(0);
  const clave = partidaIds.join(",");

  useEffect(() => {
    let activo = true;
    (async () => {
      const cambio = await cargarNombresPartida(partidaIds, apiBaseUrl, token);
      if (cambio && activo) setVersion((v) => v + 1);
    })();
    return () => {
      activo = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [clave, apiBaseUrl, token]);

  return (partidaId) => nombrePartidaEnCache(partidaId);
}
