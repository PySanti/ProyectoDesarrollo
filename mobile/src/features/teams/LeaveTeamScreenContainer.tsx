import React, { useState } from "react";
import { ScrollView, StyleSheet, Text } from "react-native";
import { useAuth } from "../../auth/AuthProvider";
import { mobileEnv } from "../../config/env";
import { LeaveTeamScreen } from "./LeaveTeamScreen";

export function LeaveTeamScreenContainer() {
  const { session } = useAuth();
  const [leftSummary, setLeftSummary] = useState<string | null>(null);

  if (!session) {
    return <Text style={styles.message}>Sesion no disponible.</Text>;
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <LeaveTeamScreen
        apiBaseUrl={mobileEnv.teamApiBaseUrl}
        token={session.token}
        onLeft={(data) => {
          const payload = data as {
            equipoId: string;
            resultado: string;
            equipoEstado: string;
          };

          setLeftSummary(
            `equipoId=${payload.equipoId} | resultado=${payload.resultado} | estado=${payload.equipoEstado}`,
          );
        }}
      />
      {leftSummary ? <Text style={styles.summary}>{leftSummary}</Text> : null}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: "#f4f7fb",
  },
  content: {
    paddingBottom: 20,
  },
  message: {
    margin: 20,
    color: "#b91c1c",
  },
  summary: {
    marginHorizontal: 20,
    marginTop: 8,
    color: "#0f172a",
    fontSize: 12,
  },
});
