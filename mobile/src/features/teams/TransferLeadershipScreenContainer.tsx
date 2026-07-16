import React from "react";
import { StyleSheet, Text } from "react-native";
import { useAuth } from "../../auth/AuthProvider";
import { mobileEnv } from "../../config/env";
import { TransferLeadershipScreen } from "./TransferLeadershipScreen";

export function TransferLeadershipScreenContainer() {
  const { session } = useAuth();

  if (!session) {
    return <Text style={styles.message}>Sesion no disponible.</Text>;
  }

  return (
    <TransferLeadershipScreen
      apiBaseUrl={mobileEnv.gatewayApiBaseUrl}
      token={session.token}
      currentUserId={session.user.sub}
    />
  );
}

const styles = StyleSheet.create({
  message: {
    margin: 20,
    color: "#b91c1c",
  },
});
