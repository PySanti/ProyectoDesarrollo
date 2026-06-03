import React from "react";
import { ActivityIndicator, Pressable, SafeAreaView, ScrollView, StyleSheet, Text, View } from "react-native";
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
  safeArea: {
    flex: 1,
    backgroundColor: "#f4f7fb",
  },
  container: {
    padding: 20,
    gap: 12,
  },
  title: {
    fontSize: 24,
    fontWeight: "700",
    color: "#0f172a",
  },
  description: {
    color: "#475569",
    fontSize: 14,
    lineHeight: 20,
  },
  filters: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 8,
  },
  filterButton: {
    borderRadius: 999,
    borderWidth: 1,
    borderColor: "#94a3b8",
    paddingHorizontal: 14,
    paddingVertical: 8,
    backgroundColor: "#ffffff",
  },
  filterButtonActive: {
    backgroundColor: "#0b5fff",
    borderColor: "#0b5fff",
  },
  filterText: {
    color: "#334155",
    fontWeight: "700",
  },
  filterTextActive: {
    color: "#ffffff",
  },
  error: {
    color: "#b91c1c",
    fontSize: 13,
  },
  empty: {
    color: "#475569",
    fontSize: 14,
  },
  card: {
    borderRadius: 14,
    backgroundColor: "#ffffff",
    borderWidth: 1,
    borderColor: "#dbe4f0",
    padding: 14,
    gap: 4,
  },
  cardTitle: {
    color: "#0f172a",
    fontSize: 17,
    fontWeight: "700",
  },
  cardLine: {
    color: "#334155",
    fontSize: 13,
  },
  joinButton: {
    marginTop: 8,
    borderRadius: 10,
    backgroundColor: "#0b5fff",
    paddingHorizontal: 12,
    paddingVertical: 10,
    alignItems: "center",
  },
  joinButtonDisabled: {
    backgroundColor: "#94a3b8",
  },
  joinButtonText: {
    color: "#ffffff",
    fontWeight: "700",
  },
});
