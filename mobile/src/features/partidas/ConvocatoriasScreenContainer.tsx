import React from "react";
import { Text } from "react-native";
import { useNavigation } from "@react-navigation/native";
import type { NativeStackNavigationProp } from "@react-navigation/native-stack";
import { useAuth } from "../../auth/AuthProvider";
import { mobileEnv } from "../../config/env";
import { AppStackParamList } from "../../navigation/types";
import { ConvocatoriasScreen } from "./ConvocatoriasScreen";

export function ConvocatoriasScreenContainer() {
  const { session } = useAuth();
  const navigation = useNavigation<NativeStackNavigationProp<AppStackParamList>>();

  if (!session) {
    return <Text>Sesion no disponible.</Text>;
  }

  return (
    <ConvocatoriasScreen
      apiBaseUrl={mobileEnv.gatewayApiBaseUrl}
      token={session.token}
      onAceptada={(partidaId, nombre) => navigation.navigate("PartidaLobby", { partidaId, nombre })}
    />
  );
}
