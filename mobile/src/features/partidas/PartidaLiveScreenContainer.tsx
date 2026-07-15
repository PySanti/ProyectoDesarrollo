import React from "react";
import { Text } from "react-native";
import { RouteProp, useNavigation, useRoute } from "@react-navigation/native";
import type { NativeStackNavigationProp } from "@react-navigation/native-stack";
import { useAuth } from "../../auth/AuthProvider";
import { mobileEnv } from "../../config/env";
import { AppStackParamList } from "../../navigation/types";
import { PartidaLiveScreen } from "./PartidaLiveScreen";

export function PartidaLiveScreenContainer() {
  const { session } = useAuth();
  const navigation = useNavigation<NativeStackNavigationProp<AppStackParamList>>();
  const route = useRoute<RouteProp<AppStackParamList, "PartidaLive">>();

  if (!session) {
    return <Text>Sesion no disponible.</Text>;
  }

  return (
    <PartidaLiveScreen
      apiBaseUrl={mobileEnv.gatewayApiBaseUrl}
      token={session.token}
      partidaId={route.params.partidaId}
      nombre={route.params.nombre}
      miSub={session.user.sub}
      onVolverAlPanel={() => navigation.navigate("PartidasPanel")}
    />
  );
}
