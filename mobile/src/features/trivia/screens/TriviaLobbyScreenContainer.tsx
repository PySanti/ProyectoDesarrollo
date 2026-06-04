import React from "react";
import { StyleSheet, Text } from "react-native";
import { NativeStackScreenProps } from "@react-navigation/native-stack";
import { useAuth } from "../../../auth/AuthProvider";
import { mobileEnv } from "../../../config/env";
import { AppStackParamList } from "../../../navigation/types";
import { TriviaLobbyScreen } from "./TriviaLobbyScreen";

type Props = NativeStackScreenProps<AppStackParamList, "TriviaLobby">;

export function TriviaLobbyScreenContainer({ route, navigation }: Props) {
  const { session } = useAuth();

  if (!session) {
    return <Text style={styles.message}>Sesion no disponible.</Text>;
  }

  return (
    <TriviaLobbyScreen
      apiBaseUrl={mobileEnv.triviaApiBaseUrl}
      token={session.token}
      partidaId={route.params.partidaId}
      onAnswer={(partidaId) => navigation.navigate("TriviaAnswer", { partidaId })}
      onScore={(partidaId) => navigation.navigate("TriviaScore", { partidaId })}
    />
  );
}

const styles = StyleSheet.create({
  message: { margin: 20, color: "#b91c1c" },
});
