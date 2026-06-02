import React from "react";
import * as ReactNative from "react-native";
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
  safeArea: {
    flex: 1,
    backgroundColor: "#f4f7fb",
  },
  container: {
    flex: 1,
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
  error: {
    color: "#b91c1c",
    fontSize: 13,
  },
  success: {
    color: "#166534",
    fontSize: 13,
  },
  noTeamCard: {
    borderRadius: 12,
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
    color: "#166534",
    fontSize: 13,
    lineHeight: 18,
  },
  button: {
    marginTop: 8,
    borderRadius: 10,
    backgroundColor: "#b91c1c",
    paddingVertical: 12,
    alignItems: "center",
    justifyContent: "center",
  },
  buttonDisabled: {
    backgroundColor: "#fca5a5",
  },
  buttonText: {
    color: "#ffffff",
    fontWeight: "700",
    fontSize: 15,
  },
});
