import React from "react";
import { StyleSheet, Text } from "react-native";
import { NativeStackScreenProps } from "@react-navigation/native-stack";
import { useAuth } from "../../auth/AuthProvider";
import { mobileEnv } from "../../config/env";
import { AppStackParamList } from "../../navigation/types";
import { BdtPublishedGamesScreen } from "./BdtPublishedGamesScreen";

type Props = NativeStackScreenProps<AppStackParamList, "BdtPublishedGames">;

export function BdtPublishedGamesScreenContainer({ navigation }: Props) {
  const { session } = useAuth();

  if (!session) {
    return <Text style={styles.message}>Sesion no disponible.</Text>;
  }

  return (
    <BdtPublishedGamesScreen
      apiBaseUrl={mobileEnv.bdtApiBaseUrl}
      token={session.token}
      onViewRanking={() => navigation.navigate("BdtRanking")}
    />
  );
}

const styles = StyleSheet.create({
  message: {
    margin: 20,
    color: "#b91c1c",
  },
});
