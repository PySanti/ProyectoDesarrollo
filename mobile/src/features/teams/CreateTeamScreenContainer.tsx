import React, { useState } from "react";
import { ScrollView, StyleSheet, Text } from "react-native";
import { useAuth } from "../../auth/AuthProvider";
import { mobileEnv } from "../../config/env";
import { CreateTeamScreen } from "./CreateTeamScreen";

export function CreateTeamScreenContainer() {
  const { session } = useAuth();
  const [createdSummary, setCreatedSummary] = useState<string | null>(null);

  if (!session) {
    return <Text style={styles.message}>Sesion no disponible.</Text>;
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <CreateTeamScreen
        apiBaseUrl={mobileEnv.teamApiBaseUrl}
        token={session.token}
        onCreated={(data) => {
          const payload = data as {
            equipoId: string;
            codigoAcceso: string;
            estado: string;
            liderUserId: string;
          };

          setCreatedSummary(
            `equipoId=${payload.equipoId} | codigoAcceso=${payload.codigoAcceso} | estado=${payload.estado} | liderUserId=${payload.liderUserId}`,
          );
        }}
      />
      {createdSummary ? <Text style={styles.summary}>{createdSummary}</Text> : null}
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
