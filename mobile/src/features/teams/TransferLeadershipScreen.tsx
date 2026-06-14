import React from "react";
import * as ReactNative from "react-native";
import { colors, fonts, radius, spacing, typography } from "../../shared/theme";
import { TransferLeadershipScreenController } from "./TransferLeadershipScreenController.js";

const { ActivityIndicator, Pressable, SafeAreaView, StyleSheet, Text, TextInput, View } = ReactNative;

type TeamMember = {
  userId: string;
  nombre?: string;
  esLider?: boolean;
};

type TransferLeadershipScreenProps = {
  apiBaseUrl: string;
  token: string;
  members?: TeamMember[];
  currentLeaderUserId?: string;
  onTransferred?: (result: unknown) => void;
};

export function TransferLeadershipScreen({
  apiBaseUrl,
  token,
  members = [],
  currentLeaderUserId,
  onTransferred,
}: TransferLeadershipScreenProps) {
  return (
    <TransferLeadershipScreenController
      apiBaseUrl={apiBaseUrl}
      token={token}
      members={members}
      currentLeaderUserId={currentLeaderUserId}
      onTransferred={onTransferred}
      components={{ ActivityIndicator, Pressable, SafeAreaView, Text, TextInput, View }}
      styles={styles}
    />
  );
}

// El controller (testeado) consume estas claves de estilo; aquí solo se actualizan los
// **valores** a tokens de marca. El input recibe el userId (cadena de máquina) → mono.
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
  memberList: {
    gap: spacing.sm,
  },
  memberButton: {
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    borderRadius: radius.md,
    padding: spacing.md,
    minHeight: 48,
    justifyContent: "center",
  },
  memberButtonActive: {
    borderColor: colors.primaryBright,
    backgroundColor: colors.primaryWash,
  },
  memberButtonText: {
    fontFamily: fonts.semibold,
    fontSize: 15,
    color: colors.ink,
  },
  input: {
    minHeight: 48,
    borderWidth: 1,
    borderColor: colors.lineStrong,
    borderRadius: radius.md,
    backgroundColor: colors.bg,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.md,
    fontFamily: fonts.mono,
    fontSize: 14,
    color: colors.ink,
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
  button: {
    minHeight: 48,
    borderRadius: radius.md,
    backgroundColor: colors.primaryFill,
    alignItems: "center",
    justifyContent: "center",
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
  },
  buttonDisabled: {
    backgroundColor: colors.primaryDisabled,
  },
  buttonText: {
    color: colors.white,
    fontFamily: fonts.semibold,
    fontSize: 15,
  },
});
