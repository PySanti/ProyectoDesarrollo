import React from "react";
import { StyleSheet, Text } from "react-native";
import { useAuth } from "../../../auth/AuthProvider";
import { mobileEnv } from "../../../config/env";
import TriviaGamesListScreen from "./TriviaGamesListScreen";

export function TriviaGamesListScreenContainer() {
  const { session } = useAuth();

  if (!session) {
    return <Text style={styles.message}>Sesion no disponible.</Text>;
  }

  return (
    <TriviaGamesListScreen
      apiBaseUrl={mobileEnv.triviaApiBaseUrl}
      token={session.token}
    />
  );
}

const styles = StyleSheet.create({
  message: {
    margin: 20,
    color: "#b91c1c",
  },
});
