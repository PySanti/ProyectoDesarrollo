import React from "react";
import { ActivityIndicator, Pressable, SafeAreaView, ScrollView, StyleSheet, Text, View } from "react-native";
import { requestBdtGeolocationPermission } from "../../permissions/bdtGeolocationPermission.js";
import { pickBdtTreasureImage, requestBdtTreasureImagePermission } from "../../permissions/bdtTreasureImagePicker.js";
import { screenStyles } from "../../shared/styles";
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

const styles = StyleSheet.create({
  safeArea: screenStyles.safeArea,
  container: screenStyles.scrollContainer,
  title: screenStyles.title,
  description: screenStyles.description,
  error: screenStyles.error,
  success: screenStyles.success,
  empty: screenStyles.empty,
  card: screenStyles.card,
  cardTitle: screenStyles.cardTitle,
  cardLine: screenStyles.cardLine,
  joinButton: screenStyles.joinButton,
  disabledButton: screenStyles.disabledButton,
  joinButtonText: screenStyles.joinButtonText,
  secondaryButton: screenStyles.secondaryButton,
  secondaryButtonText: screenStyles.secondaryButtonText,
});
