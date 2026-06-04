import React from "react";
import { ActivityIndicator, Pressable, SafeAreaView, ScrollView, StyleSheet, Text, View } from "react-native";
import { requestBdtGeolocationPermission } from "../../permissions/bdtGeolocationPermission.js";
import { screenStyles } from "../../shared/styles";
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

const styles = StyleSheet.create({
  safeArea: screenStyles.safeArea,
  container: screenStyles.scrollContainer,
  title: screenStyles.title,
  description: screenStyles.description,
  error: screenStyles.error,
  empty: screenStyles.empty,
  card: screenStyles.card,
  cardTitle: screenStyles.cardTitle,
  cardLine: screenStyles.cardLine,
  joinButton: screenStyles.joinButton,
  joinButtonText: screenStyles.joinButtonText,
});
