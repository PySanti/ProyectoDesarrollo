import React from "react";
import { ActivityIndicator } from "react-native";
import { requestBdtGeolocationPermission } from "../../permissions/bdtGeolocationPermission.js";
import { AppText, Button, Countdown, DetailRow, Hero, Notice, Panel, Stage } from "../../shared/ui";
import { game } from "../../shared/theme";
import { useBdtActiveStage } from "./useBdtActiveStage.js";

type Props = {
  apiBaseUrl: string;
  token: string;
  partidaId: string;
  onUploadTreasure?: (stageData: unknown) => void;
};

/** Forma de la etapa activa (HU-44) que entrega el modelo; el hook es `.js`, por eso se tipa aquí. */
type ActiveStageData = {
  partidaId: string;
  nombre: string;
  estado: string;
  modalidad: string;
  etapaActiva: {
    etapaId: string;
    orden: number;
    estado: string;
    tiempoLimiteSegundos: number;
  };
};

export function BdtActiveStageScreen({ apiBaseUrl, token, partidaId, onUploadTreasure }: Props) {
  const {
    loading,
    errorMessage,
    unavailableMessage,
    permissionDenied,
    permissionUnavailable,
    stageData,
    remainingSeconds,
    uploadEnabled,
  } = useBdtActiveStage({
    apiBaseUrl,
    token,
    partidaId,
    requestGeolocationPermission: requestBdtGeolocationPermission,
  }) as {
    loading: boolean;
    errorMessage: string | null;
    unavailableMessage: string | null;
    permissionDenied: boolean;
    permissionUnavailable: boolean;
    stageData: ActiveStageData | null;
    remainingSeconds: number;
    uploadEnabled: boolean;
  };

  return (
    <Stage variant="ink" gradient scroll>
      <Hero title="Etapa activa" subtitle="La ubicación es obligatoria en una BDT iniciada." onStage />

      {loading ? <ActivityIndicator color={game.onStage} /> : null}
      {permissionDenied ? (
        <Notice variant="error">
          {permissionUnavailable
            ? "La geolocalización no está disponible en este dispositivo."
            : "Debes permitir geolocalización para participar en la BDT activa."}
        </Notice>
      ) : null}
      {errorMessage ? <Notice variant="error">{errorMessage}</Notice> : null}
      {unavailableMessage ? <Notice variant="info">{unavailableMessage}</Notice> : null}

      {stageData ? (
        <>
          <Countdown seconds={remainingSeconds} label="segundos para esta etapa" />
          {remainingSeconds === 0 ? <Notice variant="error">La etapa ya expiró.</Notice> : null}

          <Panel>
            <AppText variant="title" color={game.onStage}>
              {stageData.nombre}
            </AppText>
            <DetailRow label="Estado" value={stageData.estado} onStage />
            <DetailRow label="Modalidad" value={stageData.modalidad} onStage />
            <DetailRow label="Etapa" value={String(stageData.etapaActiva.orden)} onStage />
            <DetailRow label="Estado etapa" value={stageData.etapaActiva.estado} onStage />
            <DetailRow label="Tiempo límite" value={`${stageData.etapaActiva.tiempoLimiteSegundos}s`} onStage />
          </Panel>

          {uploadEnabled ? (
            <Button label="Subir tesoro" icon="camera" onStage onPress={() => onUploadTreasure?.(stageData)} />
          ) : (
            <AppText variant="label" color={game.onStageMuted}>
              La subida de tesoro no está disponible.
            </AppText>
          )}
        </>
      ) : null}
    </Stage>
  );
}
