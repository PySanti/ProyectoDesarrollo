import React from "react";
import { StyleSheet, Text } from "react-native";
import { NativeStackScreenProps } from "@react-navigation/native-stack";
import { useAuth } from "../../auth/AuthProvider";
import { mobileEnv } from "../../config/env";
import { AppStackParamList } from "../../navigation/types";
import { BdtTreasureUploadScreen } from "./BdtTreasureUploadScreen";

type Props = NativeStackScreenProps<AppStackParamList, "BdtTreasureUpload">;

export function BdtTreasureUploadScreenContainer({ route }: Props) {
  const { session } = useAuth();

  if (!session) {
    return <Text style={styles.message}>Sesion no disponible.</Text>;
  }

  return (
    <BdtTreasureUploadScreen
      apiBaseUrl={mobileEnv.bdtApiBaseUrl}
      token={session.token}
      partidaId={route.params.partidaId}
      etapaId={route.params.etapaId}
    />
  );
}

const styles = StyleSheet.create({
  message: { margin: 20, color: "#b91c1c" },
});
