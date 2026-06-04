import React from "react";
import { Pressable, SafeAreaView, StyleSheet, Text, View } from "react-native";
import { NativeStackScreenProps } from "@react-navigation/native-stack";
import { useAuth } from "../auth/AuthProvider";
import { AppStackParamList } from "../navigation/types";
import { screenStyles } from "../shared/styles";

type Props = NativeStackScreenProps<AppStackParamList, "Home">;

export function HomeScreen({ navigation }: Props) {
  const { session, logout } = useAuth();

  return (
    <SafeAreaView style={styles.safeArea}>
      <View style={styles.container}>
        <Text style={styles.title}>Bienvenido, {session?.user.username ?? "participante"}</Text>
        <Text style={styles.subtitle}>Rol(es): {(session?.user.roles ?? []).join(", ") || "sin roles"}</Text>

        <Pressable style={styles.primaryButton} onPress={() => navigation.navigate("CreateTeam")}>
          <Text style={styles.primaryButtonText}>Ir a HU-03 Crear equipo</Text>
        </Pressable>

        <Pressable style={styles.primaryButton} onPress={() => navigation.navigate("JoinTeam")}>
          <Text style={styles.primaryButtonText}>Ir a HU-04 Unirse a equipo</Text>
        </Pressable>

        <Pressable style={styles.dangerButton} onPress={() => navigation.navigate("LeaveTeam")}>
          <Text style={styles.primaryButtonText}>Ir a HU-07 Salir del equipo</Text>
        </Pressable>

        <Pressable style={styles.primaryButton} onPress={() => navigation.navigate("TransferLeadership")}>
          <Text style={styles.primaryButtonText}>Ir a HU-06 Transferir liderazgo</Text>
        </Pressable>

        <Pressable style={styles.primaryButton} onPress={() => navigation.navigate("BdtPublishedGames")}>
          <Text style={styles.primaryButtonText}>Ir a HU-10/HU-12 Partidas BDT</Text>
        </Pressable>

        <Pressable style={styles.primaryButton} onPress={() => navigation.navigate("TriviaGamesList")}>
          <Text style={styles.primaryButtonText}>Ir a HU-09/HU-11 Partidas Trivia</Text>
        </Pressable>

        <Pressable style={styles.secondaryButton} onPress={logout}>
          <Text style={styles.secondaryButtonText}>Cerrar sesion</Text>
        </Pressable>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: screenStyles.safeArea,
  container: {
    ...screenStyles.container,
    justifyContent: "center",
  },
  title: screenStyles.title,
  subtitle: screenStyles.subtitle,
  primaryButton: screenStyles.primaryButton,
  primaryButtonText: screenStyles.primaryButtonText,
  dangerButton: screenStyles.dangerButton,
  secondaryButton: {
    ...screenStyles.secondaryButton,
    backgroundColor: "transparent",
    borderWidth: 1,
    borderColor: "#94a3b8",
  },
  secondaryButtonText: screenStyles.subtitle,
});
