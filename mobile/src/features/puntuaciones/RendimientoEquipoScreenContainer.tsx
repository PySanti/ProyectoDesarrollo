import React from "react";
import { Text } from "react-native";
import { useAuth } from "../../auth/AuthProvider";
import { mobileEnv } from "../../config/env";
import { RendimientoEquipoScreen } from "./RendimientoEquipoScreen";

export function RendimientoEquipoScreenContainer() {
  const { session } = useAuth();

  if (!session) {
    return <Text style={{ margin: 20, color: "#b91c1c" }}>Sesion no disponible.</Text>;
  }

  return <RendimientoEquipoScreen apiBaseUrl={mobileEnv.gatewayApiBaseUrl} token={session.token} />;
}
