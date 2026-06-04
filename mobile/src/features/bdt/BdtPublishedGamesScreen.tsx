import React from "react";
import { ActivityIndicator, Pressable, SafeAreaView, ScrollView, StyleSheet, Text, View } from "react-native";
import { screenStyles } from "../../shared/styles";
import { BdtPublishedGamesScreenController } from "./BdtPublishedGamesScreenController.js";

type Props = {
  apiBaseUrl: string;
  token: string;
};

export function BdtPublishedGamesScreen({ apiBaseUrl, token }: Props) {
  return (
    <BdtPublishedGamesScreenController
      apiBaseUrl={apiBaseUrl}
      token={token}
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
  filters: screenStyles.filters,
  filterButton: screenStyles.filterButton,
  filterButtonActive: screenStyles.filterButtonActive,
  filterText: screenStyles.filterText,
  filterTextActive: screenStyles.filterTextActive,
  error: screenStyles.error,
  empty: screenStyles.empty,
  card: screenStyles.card,
  cardTitle: screenStyles.cardTitle,
  cardLine: screenStyles.cardLine,
  joinButton: screenStyles.joinButton,
  joinButtonDisabled: screenStyles.joinButtonDisabled,
  joinButtonText: screenStyles.joinButtonText,
});
