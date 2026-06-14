import React from "react";
import { SafeAreaView, ScrollView, StyleSheet, View } from "react-native";
import { NativeStackScreenProps } from "@react-navigation/native-stack";
import { useAuth } from "../auth/AuthProvider";
import { AppStackParamList } from "../navigation/types";
import { AppText, Button, Card, ScreenHeader } from "../shared/ui";
import { colors, radius, spacing } from "../shared/theme";

type Props = NativeStackScreenProps<AppStackParamList, "Home">;

export function HomeScreen({ navigation }: Props) {
  const { session, logout } = useAuth();
  const roleLabel = (session?.user.roles ?? []).join(", ") || "sin roles";

  return (
    <SafeAreaView style={styles.safe}>
      <ScrollView contentContainerStyle={styles.content}>
        <ScreenHeader
          title={`Hola, ${session?.user.username ?? "participante"}`}
          subtitle="Gestiona tu equipo o entra a una partida publicada."
          right={<RoleChip label={roleLabel} />}
        />

        <Card>
          <AppText variant="title">Equipo</AppText>
          <Button label="Crear equipo" onPress={() => navigation.navigate("CreateTeam")} />
          <Button
            label="Unirse con código"
            variant="secondary"
            onPress={() => navigation.navigate("JoinTeam")}
          />
          <Button
            label="Transferir liderazgo"
            variant="secondary"
            onPress={() => navigation.navigate("TransferLeadership")}
          />
          <Button label="Salir del equipo" variant="danger" onPress={() => navigation.navigate("LeaveTeam")} />
        </Card>

        <Card>
          <AppText variant="title">Partidas</AppText>
          <Button label="Buscar tesoro" onPress={() => navigation.navigate("BdtPublishedGames")} />
          <Button label="Jugar Trivia" onPress={() => navigation.navigate("TriviaGamesList")} />
        </Card>

        <Button label="Cerrar sesión" variant="ghost" onPress={logout} />
      </ScrollView>
    </SafeAreaView>
  );
}

/** Chip neutro con el rol del participante (metadato, no estado "vivo": no usa magenta). */
function RoleChip({ label }: { label: string }) {
  return (
    <View style={styles.roleChip}>
      <AppText variant="label" color={colors.inkSoft}>
        {label}
      </AppText>
    </View>
  );
}

const styles = StyleSheet.create({
  safe: {
    flex: 1,
    backgroundColor: colors.bg,
  },
  content: {
    padding: spacing.xl,
    gap: spacing.lg,
  },
  roleChip: {
    backgroundColor: colors.surfaceSunk,
    borderWidth: 1,
    borderColor: colors.line,
    borderRadius: radius.pill,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.xs,
  },
});
