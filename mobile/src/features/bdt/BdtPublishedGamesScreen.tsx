import React from "react";
import { ActivityIndicator, Pressable, SafeAreaView, ScrollView, StyleSheet, Text, View } from "react-native";
import { cs } from "../../shared/controllerStyles";
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

// Re-skin: solo valores de marca; el controller (testeado) consume estas claves.
const styles = StyleSheet.create({
  safeArea: cs.safeArea,
  container: cs.container,
  title: cs.title,
  description: cs.description,
  filters: cs.filters,
  filterButton: cs.filterButton,
  filterButtonActive: cs.filterButtonActive,
  filterText: cs.filterText,
  filterTextActive: cs.filterTextActive,
  error: cs.error,
  empty: cs.empty,
  card: cs.card,
  cardTitle: cs.cardTitle,
  cardLine: cs.cardLine,
  joinButton: cs.primaryButton,
  joinButtonDisabled: cs.primaryButtonDisabled,
  joinButtonText: cs.primaryButtonText,
});
