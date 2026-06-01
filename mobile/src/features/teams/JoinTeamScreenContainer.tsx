import React, { useState } from "react";
import { ScrollView, StyleSheet, Text } from "react-native";
import { useAuth } from "../../auth/AuthProvider";
import { mobileEnv } from "../../config/env";
import { JoinTeamScreen } from "./JoinTeamScreen";

export function JoinTeamScreenContainer() {
  const { session } = useAuth();
  const [joinedSummary, setJoinedSummary] = useState<string | null>(null);

  if (!session) {
    return <Text style={styles.message}>Sesion no disponible.</Text>;
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <JoinTeamScreen
        apiBaseUrl={mobileEnv.teamApiBaseUrl}
        token={session.token}
        onJoined={(data) => {
          const payload = data as {
            equipoId: string;
            nombreEquipo: string;
            estado: string;
            liderUserId: string;
          };

          setJoinedSummary(
            `equipoId=${payload.equipoId} | nombre=${payload.nombreEquipo} | estado=${payload.estado} | liderUserId=${payload.liderUserId}`,
          );
        }}
      />
      {joinedSummary ? <Text style={styles.summary}>{joinedSummary}</Text> : null}
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
