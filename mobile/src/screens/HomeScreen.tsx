import React from "react";
import { StyleSheet, View } from "react-native";
import { NativeStackScreenProps } from "@react-navigation/native-stack";
import { useAuth } from "../auth/AuthProvider";
import { AppStackParamList } from "../navigation/types";
import { AppText, Hero, Icon, IconName, PressableScale, Stage } from "../shared/ui";
import { game, radius, spacing } from "../shared/theme";

type Props = NativeStackScreenProps<AppStackParamList, "Home">;

export function HomeScreen({ navigation }: Props) {
  const { session, logout } = useAuth();
  const roleLabel = (session?.user.roles ?? []).join(", ") || "sin roles";

  return (
    <Stage variant="magenta" gradient scroll>
      <Hero
        title={`Hola, ${session?.user.username ?? "participante"}`}
        subtitle="Elige tu próxima partida."
        titleVariant="display"
        onStage
        right={<RoleChip label={roleLabel} />}
      />

      <View style={styles.group}>
        <NavCard
          icon="flag"
          label="Partidas"
          sublabel="Descubre y únete a una partida"
          feature
          onPress={() => navigation.navigate("PartidasPanel")}
        />
        <NavCard
          icon="mail"
          label="Convocatorias"
          sublabel="Responde el llamado de tu equipo"
          feature
          onPress={() => navigation.navigate("Convocatorias")}
        />
        <NavCard
          icon="award"
          label="Mi historial"
          sublabel="Partidas jugadas, puntos y posición"
          feature
          onPress={() => navigation.navigate("HistorialPartidas")}
        />
      </View>

      <AppText variant="label" color={game.onStageMuted} style={styles.sectionLabel}>
        Tu equipo
      </AppText>
      <View style={styles.group}>
        <NavCard icon="plus-circle" label="Crear equipo" onPress={() => navigation.navigate("CreateTeam")} />
        <NavCard icon="mail" label="Invitaciones" onPress={() => navigation.navigate("Invitations")} />
        <NavCard icon="user-plus" label="Invitar miembro" onPress={() => navigation.navigate("InviteMember")} />
        <NavCard icon="repeat" label="Transferir liderazgo" onPress={() => navigation.navigate("TransferLeadership")} />
        <NavCard icon="log-out" label="Salir del equipo" onPress={() => navigation.navigate("LeaveTeam")} />
        <NavCard icon="trash-2" label="Eliminar equipo" onPress={() => navigation.navigate("DeleteTeam")} />
        <NavCard icon="clock" label="Historial de equipos" onPress={() => navigation.navigate("TeamHistory")} />
        <NavCard icon="award" label="Rendimiento de mi equipo" onPress={() => navigation.navigate("RendimientoEquipo")} />
      </View>

      <PressableScale onPress={logout} accessibilityRole="button" accessibilityLabel="Cerrar sesión" style={styles.signOut}>
        <Icon name="power" size={18} color={game.onStageMuted} />
        <AppText variant="label" color={game.onStageMuted}>
          Cerrar sesión
        </AppText>
      </PressableScale>
    </Stage>
  );
}

function NavCard({
  icon,
  label,
  sublabel,
  feature,
  onPress,
}: {
  icon: IconName;
  label: string;
  sublabel?: string;
  feature?: boolean;
  onPress: () => void;
}) {
  return (
    <PressableScale
      onPress={onPress}
      accessibilityRole="button"
      accessibilityLabel={label}
      style={[styles.card, feature && styles.cardFeature]}
    >
      <View style={[styles.iconWrap, feature && styles.iconWrapFeature]}>
        <Icon name={icon} size={feature ? 24 : 20} color={game.onStage} />
      </View>
      <View style={styles.cardText}>
        <AppText variant={feature ? "title" : "bodyStrong"} color={game.onStage}>
          {label}
        </AppText>
        {sublabel ? (
          <AppText variant="label" color={game.onStageMuted}>
            {sublabel}
          </AppText>
        ) : null}
      </View>
      <Icon name="chevron-right" size={20} color={game.onStageMuted} />
    </PressableScale>
  );
}

/** Chip translúcido con el rol (metadato) sobre el stage de color. */
function RoleChip({ label }: { label: string }) {
  return (
    <View style={styles.roleChip}>
      <AppText variant="label" color={game.onStage}>
        {label}
      </AppText>
    </View>
  );
}

const styles = StyleSheet.create({
  group: {
    gap: spacing.sm,
  },
  sectionLabel: {
    marginTop: spacing.sm,
  },
  card: {
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md,
    backgroundColor: game.onStageSunk,
    borderWidth: 1,
    borderColor: game.onStageLine,
    borderRadius: radius.lg,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
    minHeight: 56,
  },
  cardFeature: {
    paddingVertical: spacing.lg,
  },
  iconWrap: {
    width: 40,
    height: 40,
    borderRadius: radius.md,
    backgroundColor: game.onStageSunk,
    borderWidth: 1,
    borderColor: game.onStageLine,
    alignItems: "center",
    justifyContent: "center",
  },
  iconWrapFeature: {
    width: 48,
    height: 48,
  },
  cardText: {
    flex: 1,
    gap: 2,
  },
  roleChip: {
    backgroundColor: game.onStageSunk,
    borderWidth: 1,
    borderColor: game.onStageLine,
    borderRadius: radius.pill,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.xs,
  },
  signOut: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "center",
    gap: spacing.sm,
    minHeight: 48,
    marginTop: spacing.sm,
  },
});
