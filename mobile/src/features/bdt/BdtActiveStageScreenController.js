import React, { useEffect, useState } from "react";
import { calculateRemainingSeconds } from "./bdtActiveStageFlow.js";
import { loadActiveBdtStageFromScreen } from "./bdtActiveStageScreenModel.js";

const emptyStyles = {};

async function defaultRequestGeolocationPermission() {
  return { granted: false, unavailable: true };
}

function defaultCreateCountdownInterval(callback) {
  const intervalId = setInterval(callback, 1000);
  return () => clearInterval(intervalId);
}

/**
 * @param {{
 *   apiBaseUrl: string,
 *   token: string,
 *   partidaId: string,
 *   components: any,
 *   styles?: any,
 *   requestGeolocationPermission?: () => Promise<{ granted: boolean, unavailable?: boolean }>,
 *   realtimeClient?: { subscribe: (eventName: string, handler: (message: any) => void) => () => void },
 *   onUploadTreasure?: (stageData: any) => void,
 *   now?: () => Date,
 *   createCountdownInterval?: (callback: () => void) => () => void,
 * }} props
 */
export function BdtActiveStageScreenController({
  apiBaseUrl,
  token,
  partidaId,
  components,
  styles = emptyStyles,
  requestGeolocationPermission = defaultRequestGeolocationPermission,
  realtimeClient = undefined,
  onUploadTreasure = undefined,
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

  const { ActivityIndicator, Pressable, SafeAreaView, ScrollView, Text, View } = components;

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

  const uploadEnabled = Boolean(stageData?.puedeSubirTesoro) && !permissionDenied && !unavailableMessage && remainingSeconds > 0;

  return React.createElement(
    SafeAreaView,
    { style: styles.safeArea },
    React.createElement(
      ScrollView,
      { contentContainerStyle: styles.container },
      React.createElement(Text, { style: styles.title }, "Etapa activa BDT"),
      React.createElement(Text, { style: styles.description }, "La ubicacion es obligatoria para participar en una BDT iniciada."),
      loading ? React.createElement(ActivityIndicator, { color: "#0b5fff" }) : null,
      permissionDenied
        ? React.createElement(
            Text,
            { style: styles.error },
            permissionUnavailable
              ? "La geolocalizacion no esta disponible en este dispositivo."
              : "Debes permitir geolocalizacion para participar en la BDT activa.",
          )
        : null,
      errorMessage ? React.createElement(Text, { style: styles.error }, errorMessage) : null,
      unavailableMessage ? React.createElement(Text, { style: styles.empty }, unavailableMessage) : null,
      stageData
        ? React.createElement(
            View,
            { style: styles.card },
            React.createElement(Text, { style: styles.cardTitle }, stageData.nombre),
            React.createElement(Text, { style: styles.cardLine }, `Estado: ${stageData.estado}`),
            React.createElement(Text, { style: styles.cardLine }, `Modalidad: ${stageData.modalidad}`),
            React.createElement(Text, { style: styles.cardLine }, `Etapa: ${stageData.etapaActiva.orden}`),
            React.createElement(Text, { style: styles.cardLine }, `Estado etapa: ${stageData.etapaActiva.estado}`),
            React.createElement(Text, { style: styles.cardLine }, `Tiempo limite: ${stageData.etapaActiva.tiempoLimiteSegundos}s`),
            React.createElement(Text, { style: styles.cardLine }, `Tiempo restante: ${remainingSeconds}s`),
            remainingSeconds === 0 ? React.createElement(Text, { style: styles.error }, "La etapa ya expiro.") : null,
            uploadEnabled
              ? React.createElement(
                  Pressable,
                  {
                    accessibilityRole: "button",
                    onPress: () => onUploadTreasure?.(stageData),
                    style: styles.joinButton,
                  },
                  React.createElement(Text, { style: styles.joinButtonText }, "Subir tesoro"),
                )
              : React.createElement(Text, { style: styles.cardLine }, "La subida de tesoro no esta disponible."),
          )
        : null,
    ),
  );
}
