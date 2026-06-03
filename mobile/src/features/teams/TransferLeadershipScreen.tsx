import React from "react";
import * as ReactNative from "react-native";
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
  memberList: {
    gap: 8,
  },
  memberButton: {
    borderRadius: 10,
    borderWidth: 1,
    borderColor: "#94a3b8",
    padding: 12,
  },
  memberButtonActive: {
    borderColor: "#0b5fff",
    backgroundColor: "#dbeafe",
  },
  memberButtonText: {
    color: "#0f172a",
    fontWeight: "600",
  },
  input: {
    borderWidth: 1,
    borderColor: "#94a3b8",
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 10,
    backgroundColor: "#ffffff",
    color: "#0f172a",
  },
  error: {
    color: "#b91c1c",
    fontSize: 13,
  },
  success: {
    color: "#166534",
    fontSize: 13,
  },
  button: {
    marginTop: 8,
    borderRadius: 10,
    backgroundColor: "#0b5fff",
    paddingVertical: 12,
    alignItems: "center",
    justifyContent: "center",
  },
  buttonDisabled: {
    backgroundColor: "#93c5fd",
  },
  buttonText: {
    color: "#ffffff",
    fontWeight: "700",
    fontSize: 15,
  },
});
