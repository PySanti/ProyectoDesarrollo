// Hub de rankings (Puntuaciones, SP-4c): push aditivo sobre los GET existentes.
import { useEffect, useRef } from "react";
import { crearRankingHub } from "../../api/rankingHub";
import type { RankingConsolidadoDto, RankingJuegoDto } from "../../api/puntuacionesApi";

export interface RankingHubHandlers {
  onRankingJuego?: (payload: RankingJuegoDto) => void;
  onConsolidado?: (payload: RankingConsolidadoDto) => void;
}

export function useRankingHub(
  partidaId: string,
  accessToken: string,
  handlers: RankingHubHandlers
): void {
  const handlersRef = useRef(handlers);
  handlersRef.current = handlers;

  // El token va por ref: un refresh de sesión no debe derribar la conexión viva
  // (solo se usa en el handshake de conexión/reconexión).
  const tokenRef = useRef(accessToken);
  tokenRef.current = accessToken;

  useEffect(() => {
    if (!partidaId) return;

    const connection = crearRankingHub(() => tokenRef.current);
    let active = true;

    // "Partida no proyectada." es esperado si la proyección aún no llegó: el GET cubre.
    const suscribir = () => connection.invoke("SuscribirAPartida", partidaId).catch(() => {});

    connection.on("RankingTriviaActualizado", (p) => handlersRef.current.onRankingJuego?.(p));
    connection.on("RankingBDTActualizado", (p) => handlersRef.current.onRankingJuego?.(p));
    connection.on("RankingConsolidadoCalculado", (p) => handlersRef.current.onConsolidado?.(p));
    connection.onreconnected(() => {
      if (active) void suscribir();
    });

    connection
      .start()
      .then(() => {
        if (active) void suscribir();
      })
      .catch(() => {});

    return () => {
      active = false;
      connection.invoke("DesuscribirDePartida", partidaId).catch(() => {});
      void connection.stop().catch(() => {});
    };
  }, [partidaId]);
}
