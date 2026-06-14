import React from "react";
import * as ReactNative from "react-native";
import { colors, fonts, radius, spacing, typography } from "../../shared/theme";
import { LeaveTeamScreenController } from "./LeaveTeamScreenController.js";

const { ActivityIndicator, Pressable, SafeAreaView, StyleSheet, Text, View } = ReactNative;

type LeaveTeamScreenProps = {
  apiBaseUrl: string;
  token: string;
  onLeft?: (result: unknown) => void;
};

export function LeaveTeamScreen({ apiBaseUrl, token, onLeft }: LeaveTeamScreenProps) {
  return (
    <LeaveTeamScreenController
      apiBaseUrl={apiBaseUrl}
      token={token}
      onLeft={onLeft}
      components={{ ActivityIndicator, Pressable, SafeAreaView, Text, View }}
      styles={styles}
    />
  );
}

// El controller (testeado) consume estas claves de estilo; aquí solo se actualizan los
// **valores** a tokens de marca (la estructura y el contrato quedan intactos).
const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: colors.bg,
  },
  container: {
    flex: 1,
    padding: spacing.xl,
    gap: spacing.lg,
  },
  title: typography.display,
  description: { ...typography.body, color: colors.muted },
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
  success: {
    backgroundColor: colors.successWash,
    borderWidth: 1,
    borderColor: colors.success,
    borderRadius: radius.md,
    padding: spacing.md,
    color: "#136530",
    fontFamily: fonts.semibold,
    fontSize: 14,
  },
  noTeamCard: {
    backgroundColor: colors.successWash,
    borderWidth: 1,
    borderColor: colors.success,
    borderRadius: radius.lg,
    padding: spacing.lg,
    gap: spacing.xs,
  },
  noTeamTitle: {
    color: "#136530",
    fontFamily: fonts.semibold,
    fontSize: 16,
  },
  noTeamDescription: {
    color: "#136530",
    fontFamily: fonts.body,
    fontSize: 13,
    lineHeight: 18,
  },
  button: {
    minHeight: 48,
    borderRadius: radius.md,
    backgroundColor: colors.danger,
    alignItems: "center",
    justifyContent: "center",
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
  },
  buttonDisabled: {
    backgroundColor: "#e3a6a3",
  },
  buttonText: {
    color: colors.white,
    fontFamily: fonts.semibold,
    fontSize: 15,
  },
});
