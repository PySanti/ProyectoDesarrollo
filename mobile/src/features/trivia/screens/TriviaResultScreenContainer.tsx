import React from "react";
import { StyleSheet, Text } from "react-native";
import { NativeStackScreenProps } from "@react-navigation/native-stack";
import { useAuth } from "../../../auth/AuthProvider";
import { mobileEnv } from "../../../config/env";
import { AppStackParamList } from "../../../navigation/types";
import { TriviaResultScreen } from "./TriviaResultScreen";

type Props = NativeStackScreenProps<AppStackParamList, "TriviaResult">;

export function TriviaResultScreenContainer({ route }: Props) {
  const { session } = useAuth();

  if (!session) {
    return <Text style={styles.message}>Sesion no disponible.</Text>;
  }

  return (
    <TriviaResultScreen
      apiBaseUrl={mobileEnv.triviaApiBaseUrl}
      token={session.token}
      partidaId={route.params.partidaId}
      preguntaId={route.params.preguntaId}
    />
  );
}

const styles = StyleSheet.create({
  message: { margin: 20, color: "#b91c1c" },
});
