import { useEffect, useState } from "react";
import { getPartidas } from "../../api/partidasApi";

// Sin caché incremental ni troceo, a diferencia de useNombres: aquí no hay push de
// SignalR y `GET /partidas` trae la lista entera en un request.
export function useNombresPartida(accessToken: string): (partidaId: string) => string {
  const [nombres, setNombres] = useState<Map<string, string>>(new Map());

  useEffect(() => {
    let activo = true;
    getPartidas(accessToken)
      .then((partidas) => {
        if (!activo) return;
        setNombres(new Map(partidas.map((p) => [p.partidaId, p.nombrePartida])));
      })
      .catch(() => {
        // Degradación deliberada: las pantallas se quedan con GUIDs cortos y siguen
        // siendo operativas. Resolver un nombre nunca rompe la pantalla.
      });
    return () => {
      activo = false;
    };
  }, [accessToken]);

  return (partidaId: string) => nombres.get(partidaId) ?? partidaId.slice(0, 8);
}
