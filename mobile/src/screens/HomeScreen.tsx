import React from "react";
import { Pressable, SafeAreaView, StyleSheet, Text, View } from "react-native";
import { NativeStackScreenProps } from "@react-navigation/native-stack";
import { useAuth } from "../auth/AuthProvider";
import { AppStackParamList } from "../navigation/types";
import { screenStyles } from "../shared/styles";
import { colors, radius, spacing } from "../shared/theme";

type Props = NativeStackScreenProps<AppStackParamList, "Home">;

export function HomeScreen({ navigation }: Props) {
  const { session, logout } = useAuth();

  return (
    <SafeAreaView style={styles.safeArea}>
      <View style={styles.container}>
        <View style={styles.heroCard}>
          <Text style={styles.kicker}>App participante</Text>
          <Text style={styles.title}>Hola, {session?.user.username ?? "participante"}</Text>
          <Text style={styles.subtitle}>Elige una accion para gestionar tu equipo o entrar a una partida publicada.</Text>
          <Text style={styles.rolePill}>{(session?.user.roles ?? []).join(", ") || "sin roles"}</Text>
        </View>

        <View style={styles.sectionCard}>
          <Text style={styles.sectionTitle}>Equipo</Text>
          <Pressable style={styles.primaryButton} onPress={() => navigation.navigate("CreateTeam")}>
            <Text style={styles.primaryButtonText}>Crear equipo</Text>
          </Pressable>

          <Pressable style={styles.primaryButton} onPress={() => navigation.navigate("JoinTeam")}>
            <Text style={styles.primaryButtonText}>Unirse con codigo</Text>
          </Pressable>

          <Pressable style={styles.primaryButton} onPress={() => navigation.navigate("TransferLeadership")}>
            <Text style={styles.primaryButtonText}>Transferir liderazgo</Text>
          </Pressable>

          <Pressable style={styles.dangerButton} onPress={() => navigation.navigate("LeaveTeam")}>
            <Text style={styles.primaryButtonText}>Salir del equipo</Text>
          </Pressable>
        </View>

        <View style={styles.sectionCard}>
          <Text style={styles.sectionTitle}>Partidas</Text>
          <Pressable style={styles.primaryButton} onPress={() => navigation.navigate("BdtPublishedGames")}>
            <Text style={styles.primaryButtonText}>Buscar tesoro</Text>
          </Pressable>

          <Pressable style={styles.primaryButton} onPress={() => navigation.navigate("TriviaGamesList")}>
            <Text style={styles.primaryButtonText}>Jugar Trivia</Text>
          </Pressable>
        </View>

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
  },
  heroCard: {
    ...screenStyles.card,
    backgroundColor: colors.primaryDark,
    borderColor: colors.primaryDark,
    padding: spacing.xxl,
  },
  kicker: {
    color: colors.accent,
    fontSize: 12,
    fontWeight: "800",
    letterSpacing: 1.2,
    textTransform: "uppercase",
  },
  title: {
    ...screenStyles.title,
    color: colors.white,
  },
  subtitle: {
    ...screenStyles.subtitle,
    color: "rgba(255,255,255,0.78)",
  },
  rolePill: {
    alignSelf: "flex-start",
    marginTop: spacing.sm,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.xs,
    borderRadius: radius.pill,
    backgroundColor: "rgba(255,255,255,0.12)",
    color: colors.white,
    fontSize: 12,
    fontWeight: "800",
  },
  sectionCard: {
    ...screenStyles.card,
  },
  sectionTitle: {
    ...screenStyles.cardTitle,
    marginBottom: spacing.xs,
  },
  primaryButton: screenStyles.primaryButton,
  primaryButtonText: screenStyles.primaryButtonText,
  dangerButton: screenStyles.dangerButton,
  secondaryButton: {
    ...screenStyles.secondaryButton,
  },
  secondaryButtonText: screenStyles.secondaryButtonText,
});
