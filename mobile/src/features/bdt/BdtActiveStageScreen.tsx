import React from "react";
import { ActivityIndicator, Pressable, SafeAreaView, ScrollView, StyleSheet, Text, View } from "react-native";
import { requestBdtGeolocationPermission } from "../../permissions/bdtGeolocationPermission.js";
import { cs } from "../../shared/controllerStyles";
import { BdtActiveStageScreenController } from "./BdtActiveStageScreenController.js";

type Props = {
  apiBaseUrl: string;
  token: string;
  partidaId: string;
  onUploadTreasure?: (stageData: unknown) => void;
};

export function BdtActiveStageScreen({ apiBaseUrl, token, partidaId, onUploadTreasure }: Props) {
  return (
    <BdtActiveStageScreenController
      apiBaseUrl={apiBaseUrl}
      token={token}
      partidaId={partidaId}
      onUploadTreasure={onUploadTreasure ?? (() => undefined)}
      requestGeolocationPermission={requestBdtGeolocationPermission}
      components={{ ActivityIndicator, Pressable, SafeAreaView, ScrollView, Text, View }}
      styles={styles}
    />
  );
}

// Re-skin: solo valores de marca; el controller (testeado) consume estas claves.
const styles = StyleSheet.create({
  safeArea: cs.safeArea,
  container: cs.container,
  title: cs.title,
  description: cs.description,
  error: cs.error,
  empty: cs.empty,
  card: cs.card,
  cardTitle: cs.cardTitle,
  cardLine: cs.cardLine,
  joinButton: cs.primaryButton,
  joinButtonText: cs.primaryButtonText,
});
