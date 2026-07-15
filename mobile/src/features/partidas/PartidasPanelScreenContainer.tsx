import React from "react";
import { Text } from "react-native";
import { useNavigation } from "@react-navigation/native";
import type { NativeStackNavigationProp } from "@react-navigation/native-stack";
import { useAuth } from "../../auth/AuthProvider";
import { mobileEnv } from "../../config/env";
import { AppStackParamList } from "../../navigation/types";
import { PartidasPanelScreen } from "./PartidasPanelScreen";

export function PartidasPanelScreenContainer() {
  const { session } = useAuth();
  const navigation = useNavigation<NativeStackNavigationProp<AppStackParamList>>();

  if (!session) {
    return <Text>Sesion no disponible.</Text>;
  }

  return (
    <PartidasPanelScreen
      apiBaseUrl={mobileEnv.gatewayApiBaseUrl}
      token={session.token}
      onOpenPartida={({ partidaId, nombre }) => navigation.navigate("PartidaLobby", { partidaId, nombre })}
      onOpenMiSesion={({ partidaId, estadoPartida }) =>
        estadoPartida === "Iniciada"
          ? navigation.navigate("PartidaLive", { partidaId, nombre: "Mi partida" })
          : navigation.navigate("PartidaLobby", { partidaId, nombre: "Mi partida" })
      }
    />
  );
}
