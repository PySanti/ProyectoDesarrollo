import React from "react";
import { StyleSheet, Text } from "react-native";
import { NativeStackScreenProps } from "@react-navigation/native-stack";
import { useAuth } from "../../auth/AuthProvider";
import { mobileEnv } from "../../config/env";
import { AppStackParamList } from "../../navigation/types";
import { TeamPanelScreen } from "./TeamPanelScreen";

type Props = NativeStackScreenProps<AppStackParamList, "TeamPanel">;

export function TeamPanelScreenContainer({ navigation }: Props) {
  const { session } = useAuth();

  if (!session) {
    return <Text style={styles.message}>Sesion no disponible.</Text>;
  }

  return (
    <TeamPanelScreen
      apiBaseUrl={mobileEnv.gatewayApiBaseUrl}
      token={session.token}
      currentUserId={session.user.sub}
      navigation={navigation}
    />
  );
}

const styles = StyleSheet.create({
  message: {
    margin: 20,
    color: "#b91c1c",
  },
});
