import React from "react";
import { ActivityIndicator, StyleSheet, View } from "react-native";
import { requestBdtGeolocationPermission } from "../../permissions/bdtGeolocationPermission.js";
import { pickBdtTreasureImage, requestBdtTreasureImagePermission } from "../../permissions/bdtTreasureImagePicker.js";
import { AppText, Button, DetailRow, Hero, Icon, Notice, Panel, Reaction, Stage } from "../../shared/ui";
import { game, radius, spacing } from "../../shared/theme";
import { useBdtTreasureUpload } from "./useBdtTreasureUpload.js";

type Props = {
  apiBaseUrl: string;
  token: string;
  partidaId: string;
  etapaId: string;
};

type SelectedImage = { uri: string; name: string; type: string; size?: number };
type UploadResult = { estadoProcesamiento: string; qrDecodificado: string | null; mensaje?: string };

export function BdtTreasureUploadScreen({ apiBaseUrl, token, partidaId, etapaId }: Props) {
  const {
    imagePermissionDenied,
    geolocationDenied,
    selectedImage,
    loading,
    errorMessage,
    successMessage,
    uploadResult,
    canSubmit,
    selectImage,
    submit,
  } = useBdtTreasureUpload({
    apiBaseUrl,
    token,
    partidaId,
    etapaId,
    requestImagePermission: requestBdtTreasureImagePermission,
    requestGeolocationPermission: requestBdtGeolocationPermission,
    pickImage: pickBdtTreasureImage,
  }) as {
    imagePermissionDenied: boolean;
    geolocationDenied: boolean;
    selectedImage: SelectedImage | null;
    loading: boolean;
    errorMessage: string | null;
    successMessage: string | null;
    uploadResult: UploadResult | null;
    canSubmit: boolean;
    selectImage: () => void;
    submit: () => void;
  };

  const decoded = uploadResult?.estadoProcesamiento === "Decodificado";

  return (
    <Stage variant="magenta" gradient scroll>
      <Hero
        title="Subir tesoro QR"
        subtitle="Toma una foto nítida del QR. La validación es autoritativa del backend."
        onStage
      />

      {imagePermissionDenied ? (
        <Notice variant="error">Debes permitir cámara o imágenes para subir el tesoro QR.</Notice>
      ) : null}
      {geolocationDenied ? (
        <Notice variant="error">Debes permitir geolocalización para participar en la BDT activa.</Notice>
      ) : null}
      {errorMessage ? <Notice variant="error">{errorMessage}</Notice> : null}

      {successMessage && uploadResult ? (
        <Panel>
          <Reaction
            correct={decoded}
            title={decoded ? "QR decodificado" : "No se pudo leer el QR"}
            subtitle={uploadResult.mensaje ?? successMessage}
          />
          <DetailRow label="Estado" value={uploadResult.estadoProcesamiento} onStage />
          {uploadResult.qrDecodificado ? (
            <DetailRow label="QR" value={uploadResult.qrDecodificado} onStage />
          ) : null}
        </Panel>
      ) : null}

      {selectedImage ? (
        <Panel>
          <View style={styles.imageHead}>
            <View style={styles.thumb}>
              <Icon name="image" size={20} color={game.onStage} />
            </View>
            <AppText variant="bodyStrong" color={game.onStage} style={styles.flex}>
              {selectedImage.name}
            </AppText>
          </View>
          <DetailRow label="Tipo" value={selectedImage.type} onStage />
        </Panel>
      ) : (
        <AppText variant="label" color={game.onStageMuted}>
          Aún no has seleccionado una imagen.
        </AppText>
      )}

      {loading ? <ActivityIndicator color={game.onStage} /> : null}

      <View style={styles.actions}>
        <Button label="Tomar o seleccionar foto" icon="camera" variant="secondary" onStage onPress={selectImage} />
        <Button
          label={loading ? "Subiendo…" : "Subir tesoro"}
          icon="upload"
          onStage
          disabled={!canSubmit}
          loading={loading}
          onPress={submit}
        />
      </View>
    </Stage>
  );
}

const styles = StyleSheet.create({
  actions: {
    gap: spacing.sm,
  },
  imageHead: {
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md,
  },
  thumb: {
    width: 40,
    height: 40,
    borderRadius: radius.md,
    backgroundColor: game.onStageSunk,
    borderWidth: 1,
    borderColor: game.onStageLine,
    alignItems: "center",
    justifyContent: "center",
  },
  flex: {
    flex: 1,
  },
});
