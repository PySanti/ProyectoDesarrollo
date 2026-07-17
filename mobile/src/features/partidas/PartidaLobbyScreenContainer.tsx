import React from "react";
import { Text } from "react-native";
import { RouteProp, useNavigation, useRoute } from "@react-navigation/native";
import type { NativeStackNavigationProp } from "@react-navigation/native-stack";
import { useAuth } from "../../auth/AuthProvider";
import { mobileEnv } from "../../config/env";
import { AppStackParamList } from "../../navigation/types";
import { PartidaLobbyScreen } from "./PartidaLobbyScreen";

export function PartidaLobbyScreenContainer() {
  const { session } = useAuth();
  const route = useRoute<RouteProp<AppStackParamList, "PartidaLobby">>();
  const navigation = useNavigation<NativeStackNavigationProp<AppStackParamList>>();

  if (!session) {
    return <Text>Sesion no disponible.</Text>;
  }

  return (
    <PartidaLobbyScreen
      apiBaseUrl={mobileEnv.gatewayApiBaseUrl}
      token={session.token}
      partidaId={route.params.partidaId}
      nombre={route.params.nombre}
      onIniciada={() => navigation.replace("PartidaLive", { partidaId: route.params.partidaId, nombre: route.params.nombre })}
    />
  );
}
