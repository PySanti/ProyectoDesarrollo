import { useEffect, useState } from "react";
import { calculateRemainingSeconds } from "./bdtActiveStageFlow.js";
import { loadActiveBdtStageFromScreen } from "./bdtActiveStageScreenModel.js";

async function defaultRequestGeolocationPermission() {
  return { granted: false, unavailable: true };
}

function defaultCreateCountdownInterval(callback) {
  const intervalId = setInterval(callback, 1000);
  return () => clearInterval(intervalId);
}

/**
 * Orquestación de la pantalla de etapa activa de BDT (HU-44), extraída del antiguo controller para que la
 * UI sea presentacional (registro de juego) y la lógica siga siendo **testeable** sin acoplarse al render.
 * Mantiene las dependencias inyectables (permiso, realtime, reloj, intervalo) que usan los tests:
 *   - gatea el fetch tras el permiso de geolocalización (denegado ⇒ no consulta la etapa),
 *   - refresca solo ante el evento documentado `PartidaBDTIniciada` de la misma partida,
 *   - corre una cuenta regresiva (intervalo de 1s) derivando `remainingSeconds` de `cierraEnUtc`,
 *   - deriva `uploadEnabled` (puede subir + permiso + no expirada).
 *
 * @param {{
 *   apiBaseUrl: string, token: string, partidaId: string,
 *   requestGeolocationPermission?: () => Promise<{ granted: boolean, unavailable?: boolean }>,
 *   realtimeClient?: { subscribe: (eventName: string, handler: (message: any) => void) => () => void },
 *   now?: () => Date,
 *   createCountdownInterval?: (callback: () => void) => () => void,
 * }} props
 */
export function useBdtActiveStage({
  apiBaseUrl,
  token,
  partidaId,
  requestGeolocationPermission = defaultRequestGeolocationPermission,
  realtimeClient = undefined,
  now = () => new Date(),
  createCountdownInterval = defaultCreateCountdownInterval,
}) {
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState(null);
  const [unavailableMessage, setUnavailableMessage] = useState(null);
  const [permissionDenied, setPermissionDenied] = useState(false);
  const [permissionUnavailable, setPermissionUnavailable] = useState(false);
  const [stageData, setStageData] = useState(null);
  const [currentTime, setCurrentTime] = useState(() => now());

  const load = () =>
    loadActiveBdtStageFromScreen({
      apiBaseUrl,
      token,
      partidaId,
      setLoading,
      setErrorMessage,
      setUnavailableMessage,
      setStageData,
    });

  useEffect(() => {
    let active = true;

    requestGeolocationPermission().then((permission) => {
      if (!active) {
        return;
      }

      if (!permission?.granted) {
        setPermissionDenied(true);
        setPermissionUnavailable(Boolean(permission?.unavailable));
        setStageData(null);
        return;
      }

      setPermissionDenied(false);
      setPermissionUnavailable(false);
      void load();
    });

    return () => {
      active = false;
    };
  }, [apiBaseUrl, token, partidaId]);

  useEffect(() => {
    if (!realtimeClient?.subscribe) {
      return undefined;
    }

    return realtimeClient.subscribe("PartidaBDTIniciada", (message) => {
      if (message?.partidaId === partidaId) {
        void load();
      }
    });
  }, [realtimeClient, partidaId]);

  useEffect(() => {
    if (!stageData?.etapaActiva?.cierraEnUtc) {
      return undefined;
    }

    setCurrentTime(now());
    return createCountdownInterval(() => {
      setCurrentTime(now());
    });
  }, [stageData?.etapaActiva?.cierraEnUtc]);

  const remainingSeconds = stageData?.etapaActiva?.cierraEnUtc
    ? calculateRemainingSeconds(stageData.etapaActiva.cierraEnUtc, currentTime)
    : 0;

  const uploadEnabled =
    Boolean(stageData?.puedeSubirTesoro) && !permissionDenied && !unavailableMessage && remainingSeconds > 0;

  return {
    loading,
    errorMessage,
    unavailableMessage,
    permissionDenied,
    permissionUnavailable,
    stageData,
    remainingSeconds,
    uploadEnabled,
  };
}
