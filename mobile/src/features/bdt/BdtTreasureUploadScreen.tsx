import React from "react";
import { ActivityIndicator, Pressable, SafeAreaView, ScrollView, StyleSheet, Text, View } from "react-native";
import { requestBdtGeolocationPermission } from "../../permissions/bdtGeolocationPermission.js";
import { pickBdtTreasureImage, requestBdtTreasureImagePermission } from "../../permissions/bdtTreasureImagePicker.js";
import { cs } from "../../shared/controllerStyles";
import { BdtTreasureUploadScreenController } from "./BdtTreasureUploadScreenController.js";

type Props = {
  apiBaseUrl: string;
  token: string;
  partidaId: string;
  etapaId: string;
};

export function BdtTreasureUploadScreen({ apiBaseUrl, token, partidaId, etapaId }: Props) {
  return (
    <BdtTreasureUploadScreenController
      apiBaseUrl={apiBaseUrl}
      token={token}
      partidaId={partidaId}
      etapaId={etapaId}
      components={{ ActivityIndicator, Pressable, SafeAreaView, ScrollView, Text, View }}
      styles={styles}
      requestImagePermission={requestBdtTreasureImagePermission}
      requestGeolocationPermission={requestBdtGeolocationPermission}
      pickImage={pickBdtTreasureImage}
    />
  );
}

// Re-skin: solo valores de marca; el controller (testeado) consume estas claves.
// `disabledButton` se usa como estilo completo del botón (no override), por eso lleva relleno.
const styles = StyleSheet.create({
  safeArea: cs.safeArea,
  container: cs.container,
  title: cs.title,
  description: cs.description,
  error: cs.error,
  success: cs.success,
  empty: cs.empty,
  card: cs.card,
  cardTitle: cs.cardTitle,
  cardLine: cs.cardLine,
  joinButton: cs.primaryButton,
  disabledButton: cs.primaryButtonDisabledFill,
  joinButtonText: cs.primaryButtonText,
  secondaryButton: cs.secondaryButton,
  secondaryButtonText: cs.secondaryButtonText,
});
