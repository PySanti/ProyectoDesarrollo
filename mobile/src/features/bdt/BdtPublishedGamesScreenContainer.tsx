import React from "react";
import { StyleSheet, Text } from "react-native";
import { useAuth } from "../../auth/AuthProvider";
import { mobileEnv } from "../../config/env";
import { BdtPublishedGamesScreen } from "./BdtPublishedGamesScreen";

export function BdtPublishedGamesScreenContainer() {
  const { session } = useAuth();

  if (!session) {
    return <Text style={styles.message}>Sesion no disponible.</Text>;
  }

  return <BdtPublishedGamesScreen apiBaseUrl={mobileEnv.bdtApiBaseUrl} token={session.token} />;
}

const styles = StyleSheet.create({
  message: {
    margin: 20,
    color: "#b91c1c",
  },
});
