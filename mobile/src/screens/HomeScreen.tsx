import React from "react";
import { Pressable, SafeAreaView, StyleSheet, Text, View } from "react-native";
import { NativeStackScreenProps } from "@react-navigation/native-stack";
import { useAuth } from "../auth/AuthProvider";
import { AppStackParamList } from "../navigation/types";

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

        <Pressable style={styles.secondaryButton} onPress={logout}>
          <Text style={styles.secondaryButtonText}>Cerrar sesion</Text>
        </Pressable>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: "#f4f7fb",
  },
  container: {
    flex: 1,
    padding: 24,
    gap: 12,
    justifyContent: "center",
  },
  title: {
    fontSize: 24,
    fontWeight: "700",
    color: "#0f172a",
  },
  subtitle: {
    color: "#334155",
    fontSize: 14,
  },
  primaryButton: {
    marginTop: 8,
    borderRadius: 10,
    backgroundColor: "#0b5fff",
    paddingVertical: 12,
    alignItems: "center",
  },
  primaryButtonText: {
    color: "#fff",
    fontWeight: "700",
  },
  secondaryButton: {
    borderRadius: 10,
    borderWidth: 1,
    borderColor: "#94a3b8",
    paddingVertical: 12,
    alignItems: "center",
  },
  secondaryButtonText: {
    color: "#334155",
    fontWeight: "600",
  },
});
