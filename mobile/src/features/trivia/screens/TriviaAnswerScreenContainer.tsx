import React from "react";
import { StyleSheet, Text } from "react-native";
import { NativeStackScreenProps } from "@react-navigation/native-stack";
import { useAuth } from "../../../auth/AuthProvider";
import { mobileEnv } from "../../../config/env";
import { AppStackParamList } from "../../../navigation/types";
import { TriviaAnswerScreen } from "./TriviaAnswerScreen";

type Props = NativeStackScreenProps<AppStackParamList, "TriviaAnswer">;

export function TriviaAnswerScreenContainer({ route, navigation }: Props) {
  const { session } = useAuth();

  if (!session) {
    return <Text style={styles.message}>Sesion no disponible.</Text>;
  }

  return (
    <TriviaAnswerScreen
      apiBaseUrl={mobileEnv.triviaApiBaseUrl}
      token={session.token}
      partidaId={route.params.partidaId}
      onResult={(partidaId, preguntaId) => navigation.navigate("TriviaResult", { partidaId, preguntaId })}
    />
  );
}

const styles = StyleSheet.create({
  message: { margin: 20, color: "#b91c1c" },
});
