import React from "react";
import { StyleSheet, Text } from "react-native";
import { NativeStackScreenProps } from "@react-navigation/native-stack";
import { useAuth } from "../../auth/AuthProvider";
import { mobileEnv } from "../../config/env";
import { AppStackParamList } from "../../navigation/types";
import { BdtActiveStageScreen } from "./BdtActiveStageScreen";
import { buildBdtTreasureUploadParams } from "./bdtActiveStageNavigation";

type Props = NativeStackScreenProps<AppStackParamList, "BdtActiveStage">;

export function BdtActiveStageScreenContainer({ route, navigation }: Props) {
  const { session } = useAuth();

  if (!session) {
    return <Text style={styles.message}>Sesion no disponible.</Text>;
  }

  return (
    <BdtActiveStageScreen
      apiBaseUrl={mobileEnv.bdtApiBaseUrl}
      token={session.token}
      partidaId={route.params.partidaId}
      onUploadTreasure={(stageData: any) => {
        navigation.navigate("BdtTreasureUpload", buildBdtTreasureUploadParams(stageData));
      }}
    />
  );
}

const styles = StyleSheet.create({
  message: {
    margin: 20,
    color: "#b91c1c",
  },
});
