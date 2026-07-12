import React from "react";
import * as ReactNative from "react-native";
import { colors, fonts, radius, spacing, typography } from "../../shared/theme";
import { TeamHistoryScreenController } from "./TeamHistoryScreenController.js";

const { ActivityIndicator, ScrollView, SafeAreaView, StyleSheet, Text, View } = ReactNative;

type TeamHistoryScreenProps = {
  apiBaseUrl: string;
  token: string;
};

export function TeamHistoryScreen({ apiBaseUrl, token }: TeamHistoryScreenProps) {
  return (
    <TeamHistoryScreenController
      apiBaseUrl={apiBaseUrl}
      token={token}
      components={{ ActivityIndicator, ScrollView, SafeAreaView, Text, View }}
      styles={styles}
    />
  );
}

// El controller (testeado) consume estas claves de estilo; aquí solo se actualizan los
// **valores** a tokens de marca (la estructura y el contrato quedan intactos).
const baseStyles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: colors.bg,
  },
  container: {
    padding: spacing.xl,
    gap: spacing.lg,
  },
  title: typography.display,
  description: { ...typography.body, color: colors.muted },
  loadingIndicator: {
    marginTop: spacing.lg,
  },
  error: {
    backgroundColor: colors.dangerWash,
    borderWidth: 1,
    borderColor: colors.danger,
    borderRadius: radius.md,
    padding: spacing.md,
    color: colors.danger,
    fontFamily: fonts.semibold,
    fontSize: 14,
  },
  empty: {
    ...typography.body,
    color: colors.muted,
    textAlign: "center",
    marginTop: spacing.lg,
  },
  list: {
    gap: spacing.md,
  },
  item: {
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.lineStrong,
    borderRadius: radius.lg,
    padding: spacing.lg,
    gap: spacing.xs,
  },
  itemName: {
    fontFamily: fonts.semibold,
    fontSize: 16,
    color: colors.ink,
  },
  itemDate: {
    ...typography.label,
    color: colors.muted,
  },
});

const styles = { ...baseStyles, loadingIndicatorColor: colors.primaryBright };
