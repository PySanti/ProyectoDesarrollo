import { useEffect, useRef } from "react";
import { crearSesionHub } from "../../api/sesionHub";

export interface SesionHubHandlers {
  onEnLobby?: (payload: { partidaId: string }) => void;
  onIniciada?: (payload: { partidaId: string }) => void;
  onCancelada?: (payload: { partidaId: string; motivo?: string }) => void;
  onJuegoActivado?: (payload: {
    partidaId: string;
    juegoId: string;
    orden: number;
    tipoJuego: string;
  }) => void;
  onFinalizada?: (payload: { partidaId: string }) => void;
  onPreguntaActivada?: (payload: {
    partidaId: string;
    juegoId: string;
    preguntaId: string;
    orden: number;
    fechaLimiteUtc: string;
  }) => void;
  onPreguntaCerrada?: (payload: { partidaId: string; juegoId: string; preguntaId: string }) => void;
  onEtapaActivada?: (payload: {
    partidaId: string;
    juegoId: string;
    etapaId: string;
    orden: number;
    fechaLimiteUtc: string;
  }) => void;
  onEtapaCerrada?: (payload: {
    partidaId: string;
    juegoId: string;
    etapaId: string;
    ganadorParticipanteId?: string;
    ganadorEquipoId?: string;
  }) => void;
  onEtapaGanada?: (payload: {
    partidaId: string;
    juegoId: string;
    etapaId: string;
    ganadorParticipanteId?: string;
    ganadorEquipoId?: string;
  }) => void;
  onUbicacionActualizada?: (payload: {
    partidaId: string;
    participanteId: string;
    latitud: number;
    longitud: number;
    timestampUtc: string;
  }) => void;
}

export function useSesionHub(
  partidaId: string,
  accessToken: string,
  handlers: SesionHubHandlers
): void {
  // Ref para no reconstruir la conexion cuando la pagina pasa handlers inline nuevos en cada render.
  const handlersRef = useRef(handlers);
  handlersRef.current = handlers;

  // El token va por ref: un refresh de sesión no debe derribar la conexión viva
  // (solo se usa en el handshake de conexión/reconexión).
  const tokenRef = useRef(accessToken);
  tokenRef.current = accessToken;

  useEffect(() => {
    if (!partidaId) return;

    const connection = crearSesionHub(() => tokenRef.current);
    let active = true;

    const suscribir = () => connection.invoke("SuscribirAPartida", partidaId).catch(() => {});

    connection.on("PartidaEnLobby", (p) => handlersRef.current.onEnLobby?.(p));
    connection.on("PartidaIniciada", (p) => handlersRef.current.onIniciada?.(p));
    connection.on("PartidaCancelada", (p) => handlersRef.current.onCancelada?.(p));
    connection.on("JuegoActivado", (p) => handlersRef.current.onJuegoActivado?.(p));
    connection.on("PartidaFinalizada", (p) => handlersRef.current.onFinalizada?.(p));
    connection.on("PreguntaActivada", (p) => handlersRef.current.onPreguntaActivada?.(p));
    connection.on("PreguntaCerrada", (p) => handlersRef.current.onPreguntaCerrada?.(p));
    connection.on("EtapaActivada", (p) => handlersRef.current.onEtapaActivada?.(p));
    connection.on("EtapaCerrada", (p) => handlersRef.current.onEtapaCerrada?.(p));
    connection.on("EtapaGanada", (p) => handlersRef.current.onEtapaGanada?.(p));
    connection.on("UbicacionActualizada", (p) => handlersRef.current.onUbicacionActualizada?.(p));
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
