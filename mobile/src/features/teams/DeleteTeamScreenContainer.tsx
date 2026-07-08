import React from "react";
import { ScrollView, StyleSheet, Text } from "react-native";
import { NativeStackScreenProps } from "@react-navigation/native-stack";
import { useAuth } from "../../auth/AuthProvider";
import { mobileEnv } from "../../config/env";
import { AppStackParamList } from "../../navigation/types";
import { DeleteTeamScreen } from "./DeleteTeamScreen";

type Props = NativeStackScreenProps<AppStackParamList, "DeleteTeam">;

export function DeleteTeamScreenContainer({ navigation }: Props) {
  const { session } = useAuth();

  if (!session) {
    return <Text style={styles.message}>Sesion no disponible.</Text>;
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <DeleteTeamScreen
        apiBaseUrl={mobileEnv.identityApiBaseUrl}
        token={session.token}
        onDeleted={() => navigation.navigate("Home")}
      />
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
