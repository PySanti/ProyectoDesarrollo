import React from "react";
import { StyleSheet, Text } from "react-native";
import { NativeStackScreenProps } from "@react-navigation/native-stack";
import { useAuth } from "../../../auth/AuthProvider";
import { mobileEnv } from "../../../config/env";
import { AppStackParamList } from "../../../navigation/types";
import TriviaGamesListScreen from "./TriviaGamesListScreen";

type Props = NativeStackScreenProps<AppStackParamList, "TriviaGamesList">;

export function TriviaGamesListScreenContainer({ navigation }: Props) {
  const { session } = useAuth();

  if (!session) {
    return <Text style={styles.message}>Sesion no disponible.</Text>;
  }

  return (
    <TriviaGamesListScreen
      apiBaseUrl={mobileEnv.triviaApiBaseUrl}
      token={session.token}
      onOpenLobby={(partidaId) => navigation.navigate("TriviaLobby", { partidaId })}
      onPlayDemo={() => navigation.navigate("TriviaLivePlay", { partidaId: "demo-partida" })}
    />
  );
}

const styles = StyleSheet.create({
  message: {
    margin: 20,
    color: "#b91c1c",
  },
});
