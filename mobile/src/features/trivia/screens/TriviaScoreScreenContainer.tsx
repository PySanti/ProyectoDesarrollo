import React from "react";
import { StyleSheet, Text } from "react-native";
import { NativeStackScreenProps } from "@react-navigation/native-stack";
import { useAuth } from "../../../auth/AuthProvider";
import { mobileEnv } from "../../../config/env";
import { AppStackParamList } from "../../../navigation/types";
import { TriviaScoreScreen } from "./TriviaScoreScreen";

type Props = NativeStackScreenProps<AppStackParamList, "TriviaScore">;

export function TriviaScoreScreenContainer({ route }: Props) {
  const { session } = useAuth();

  if (!session) {
    return <Text style={styles.message}>Sesion no disponible.</Text>;
  }

  return <TriviaScoreScreen apiBaseUrl={mobileEnv.triviaApiBaseUrl} token={session.token} partidaId={route.params.partidaId} />;
}

const styles = StyleSheet.create({
  message: { margin: 20, color: "#b91c1c" },
});
