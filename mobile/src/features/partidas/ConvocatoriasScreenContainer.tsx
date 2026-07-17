import React from "react";
import { Text } from "react-native";
import { useAuth } from "../../auth/AuthProvider";
import { mobileEnv } from "../../config/env";
import { ConvocatoriasScreen } from "./ConvocatoriasScreen";

export function ConvocatoriasScreenContainer() {
  const { session } = useAuth();

  if (!session) {
    return <Text>Sesion no disponible.</Text>;
  }

  return <ConvocatoriasScreen apiBaseUrl={mobileEnv.gatewayApiBaseUrl} token={session.token} />;
}
