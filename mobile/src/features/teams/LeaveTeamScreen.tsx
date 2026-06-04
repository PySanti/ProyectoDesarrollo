import React from "react";
import * as ReactNative from "react-native";
import { colors } from "../../shared/theme";
import { screenStyles } from "../../shared/styles";
import { LeaveTeamScreenController } from "./LeaveTeamScreenController.js";

const { ActivityIndicator, Pressable, SafeAreaView, StyleSheet, Text, View } = ReactNative;

type LeaveTeamScreenProps = {
  apiBaseUrl: string;
  token: string;
  onLeft?: (result: unknown) => void;
};

export function LeaveTeamScreen({ apiBaseUrl, token, onLeft }: LeaveTeamScreenProps) {
  return <LeaveTeamScreenController apiBaseUrl={apiBaseUrl} token={token} onLeft={onLeft} components={{ ActivityIndicator, Pressable, SafeAreaView, Text, View }} styles={styles} />;
}

const styles = StyleSheet.create({
  safeArea: screenStyles.safeArea,
  container: screenStyles.container,
  title: screenStyles.title,
  description: screenStyles.description,
  error: screenStyles.error,
  success: screenStyles.success,
  noTeamCard: {
    ...screenStyles.card,
    backgroundColor: "#dcfce7",
    padding: 14,
    gap: 4,
  },
  noTeamTitle: {
    color: "#14532d",
    fontSize: 16,
    fontWeight: "700",
  },
  noTeamDescription: {
    color: colors.success,
    fontSize: 13,
    lineHeight: 18,
  },
  button: screenStyles.dangerButton,
  buttonDisabled: {
    backgroundColor: "#fca5a5",
  },
  buttonText: screenStyles.primaryButtonText,
});
