import React, { useCallback, useState } from "react";
import { ActivityIndicator, SafeAreaView, ScrollView, StyleSheet, View } from "react-native";
import { useFocusEffect } from "@react-navigation/native";
import { NativeStackNavigationProp } from "@react-navigation/native-stack";
import { AppText, Button, Card, Notice, ScreenHeader } from "../../shared/ui";
import { colors, radius, spacing } from "../../shared/theme";
import { AppStackParamList } from "../../navigation/types";
import { fetchMyTeamStatus } from "./teamPanelFlow.js";

type Participante = { usuarioId: string; nombre: string; esLider: boolean };

type TeamStatus =
  | { status: "sinEquipo" }
  | { status: "lider" | "miembro"; nombreEquipo: string; participantes: Participante[] };

type TeamPanelNavigation = NativeStackNavigationProp<AppStackParamList, "TeamPanel">;

type TeamPanelScreenProps = {
  apiBaseUrl: string;
  token: string;
  currentUserId: string;
  navigation: TeamPanelNavigation;
};

// `fetchMyTeamStatus` (teamPanelFlow.js) has no JSDoc, so TS infers/widens its return type from the
// plain object literals instead of preserving the `ok` discriminant. Annotate the real shape here.
type FetchTeamStatusResult =
  | { ok: false; type?: string; message?: string }
  | { ok: true; status: "sinEquipo" }
  | {
      ok: true;
      status: "lider" | "miembro";
      equipoId: string;
      nombreEquipo: string;
      participantes: Participante[];
    };

export function TeamPanelScreen({ apiBaseUrl, token, currentUserId, navigation }: TeamPanelScreenProps) {
  const [loading, setLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [team, setTeam] = useState<TeamStatus | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setErrorMessage(null);
    const result = (await fetchMyTeamStatus({
      apiBaseUrl,
      token,
      currentUserId,
      fetchImpl: undefined,
    })) as FetchTeamStatusResult;
    setLoading(false);
    if (!result.ok) {
      setErrorMessage(result.message ?? "No se pudo cargar tu equipo.");
      return;
    }
    if (result.status === "sinEquipo") {
      setTeam({ status: "sinEquipo" });
      return;
    }
    setTeam({
      status: result.status,
      nombreEquipo: result.nombreEquipo as string,
      participantes: result.participantes as Participante[],
    });
  }, [apiBaseUrl, token, currentUserId]);

  useFocusEffect(
    useCallback(() => {
      load();
    }, [load])
  );

  return (
    <SafeAreaView style={styles.safe}>
      <ScrollView contentContainerStyle={styles.content}>
        <ScreenHeader title="Gestión de equipo" subtitle="Tu equipo, tus compañeros y tus acciones." />
        {errorMessage ? <Notice variant="error">{errorMessage}</Notice> : null}
        {loading ? (
          <ActivityIndicator color={colors.primaryBright} size="large" />
        ) : team?.status === "sinEquipo" ? (
          <SinEquipoView navigation={navigation} />
        ) : team ? (
          <ConEquipoView team={team} navigation={navigation} />
        ) : null}
      </ScrollView>
    </SafeAreaView>
  );
}

function SinEquipoView({ navigation }: { navigation: TeamPanelNavigation }) {
  return (
    <View style={styles.group}>
      <Card>
        <AppText variant="body" color={colors.muted}>
          No pertenecés a ningún equipo activo.
        </AppText>
      </Card>
      <Button label="Crear equipo" onPress={() => navigation.navigate("CreateTeam")} />
      <Button label="Invitaciones" variant="secondary" onPress={() => navigation.navigate("Invitations")} />
      <Button label="Historial de equipos" variant="secondary" onPress={() => navigation.navigate("TeamHistory")} />
    </View>
  );
}

function ConEquipoView({
  team,
  navigation,
}: {
  team: Extract<TeamStatus, { status: "lider" | "miembro" }>;
  navigation: TeamPanelNavigation;
}) {
  const esLider = team.status === "lider";
  return (
    <View style={styles.group}>
      <Card>
        <View style={styles.teamHeader}>
          <AppText variant="title">{team.nombreEquipo}</AppText>
          <RoleBadge label={esLider ? "Líder" : "Miembro"} />
        </View>
        <View style={styles.membersList}>
          {team.participantes.map((p) => (
            <View key={p.usuarioId} style={styles.memberRow}>
              <AppText variant="body">{p.nombre || "Sin nombre"}</AppText>
              {p.esLider ? <RoleBadge label="Líder" /> : null}
            </View>
          ))}
        </View>
      </Card>

      <Button label="Invitaciones" onPress={() => navigation.navigate("Invitations")} />
      {esLider ? <Button label="Invitar miembro" onPress={() => navigation.navigate("InviteMember")} /> : null}
      {esLider ? (
        <Button
          label="Transferir liderazgo"
          variant="secondary"
          onPress={() => navigation.navigate("TransferLeadership")}
        />
      ) : null}
      <Button label="Salir del equipo" variant="secondary" onPress={() => navigation.navigate("LeaveTeam")} />
      {esLider ? (
        <Button label="Eliminar equipo" variant="danger" onPress={() => navigation.navigate("DeleteTeam")} />
      ) : null}
      <Button label="Historial de equipos" variant="secondary" onPress={() => navigation.navigate("TeamHistory")} />
      <Button
        label="Rendimiento de equipo"
        variant="secondary"
        onPress={() => navigation.navigate("RendimientoEquipo")}
      />
    </View>
  );
}

function RoleBadge({ label }: { label: string }) {
  return (
    <View style={styles.badge}>
      <AppText variant="label" color={colors.primaryStrong}>
        {label}
      </AppText>
    </View>
  );
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: colors.bg },
  content: { padding: spacing.xl, gap: spacing.lg },
  group: { gap: spacing.sm },
  teamHeader: { flexDirection: "row", justifyContent: "space-between", alignItems: "center" },
  membersList: { gap: spacing.xs, marginTop: spacing.sm },
  memberRow: { flexDirection: "row", justifyContent: "space-between", alignItems: "center" },
  badge: {
    backgroundColor: colors.primaryWash,
    borderRadius: radius.pill,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.xs,
  },
});
