import React from "react";
import * as ReactNative from "react-native";
import { colors } from "../../shared/theme";
import { screenStyles } from "../../shared/styles";
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

const styles = StyleSheet.create({
  safeArea: screenStyles.safeArea,
  container: screenStyles.container,
  title: screenStyles.title,
  description: screenStyles.description,
  memberList: {
    gap: 8,
  },
  memberButton: {
    ...screenStyles.card,
    borderRadius: 10,
    borderWidth: 1,
    padding: 12,
  },
  memberButtonActive: {
    borderColor: colors.primary,
    backgroundColor: colors.primaryMuted,
  },
  memberButtonText: screenStyles.cardTitle,
  input: screenStyles.input,
  error: screenStyles.error,
  success: screenStyles.success,
  button: screenStyles.primaryButton,
  buttonDisabled: screenStyles.primaryButtonDisabled,
  buttonText: screenStyles.primaryButtonText,
});
