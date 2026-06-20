import React from "react";
import { ScrollView, StyleSheet, Text } from "react-native";
import { useAuth } from "../../auth/AuthProvider";
import { mobileEnv } from "../../config/env";
import { LeaveTeamScreen } from "./LeaveTeamScreen";

export function LeaveTeamScreenContainer() {
  const { session } = useAuth();

  if (!session) {
    return <Text style={styles.message}>Sesion no disponible.</Text>;
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <LeaveTeamScreen apiBaseUrl={mobileEnv.teamApiBaseUrl} token={session.token} />
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
});
