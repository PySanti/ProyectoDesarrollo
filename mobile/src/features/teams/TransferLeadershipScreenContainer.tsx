import React, { useState } from "react";
import { ScrollView, StyleSheet, Text } from "react-native";
import { useAuth } from "../../auth/AuthProvider";
import { mobileEnv } from "../../config/env";
import { TransferLeadershipScreen } from "./TransferLeadershipScreen";

export function TransferLeadershipScreenContainer() {
  const { session } = useAuth();
  const [transferSummary, setTransferSummary] = useState<string | null>(null);

  if (!session) {
    return <Text style={styles.message}>Sesion no disponible.</Text>;
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <TransferLeadershipScreen
        apiBaseUrl={mobileEnv.teamApiBaseUrl}
        token={session.token}
        currentLeaderUserId={session.user.sub}
        onTransferred={(data) => {
          const payload = data as {
            equipoId: string;
            liderAnteriorUserId: string;
            nuevoLiderUserId: string;
            equipoEstado: string;
          };

          setTransferSummary(
            `equipoId=${payload.equipoId} | liderAnterior=${payload.liderAnteriorUserId} | nuevoLider=${payload.nuevoLiderUserId} | estado=${payload.equipoEstado}`,
          );
        }}
      />
      {transferSummary ? <Text style={styles.summary}>{transferSummary}</Text> : null}
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
