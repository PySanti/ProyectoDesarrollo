import React, { useCallback, useEffect, useRef, useState } from "react";
import { ActivityIndicator, ScrollView, StyleSheet } from "react-native";
import { AppText, Button, Card, Notice, ScreenHeader } from "../../shared/ui";
import { colors, spacing } from "../../shared/theme";
import { cargarLobby, accionParticipacion } from "./partidaLobbyFlow.js";
import { crearSesionHub } from "./sesionHub.js";

type Lobby = {
  partidaId: string;
  estado: string;
  modalidad: "Individual" | "Equipo";
  minimosParticipacion: number;
  maximosParticipacion: number;
  inscritosActivos: number;
};

type Props = {
  apiBaseUrl: string;
  token: string;
  partidaId: string;
  nombre: string;
  onIniciada: () => void;
};

type Aviso = { variant: "info" | "error" | "success"; texto: string } | null;

// cargarLobby/accionParticipacion viven en un .js sin checkJs: TS pierde el discriminante
// literal de "ok" al inferir su tipo exportado. Se declara aquí la forma real (ver
// partidaLobbyFlow.js) y se castea una vez por llamada en lugar de perder narrowing.
type LobbyResult =
  | { ok: true; lobby: Lobby; inscrito: boolean; esLider: boolean }
  | { ok: false; type: string; message?: string };

type AccionResult = { ok: true; data?: unknown } | { ok: false; type: string; message?: string };

export function PartidaLobbyScreen({ apiBaseUrl, token, partidaId, nombre, onIniciada }: Props) {
  const [lobby, setLobby] = useState<Lobby | null>(null);
  const [inscrito, setInscrito] = useState(false);
  const [esLider, setEsLider] = useState(true);
  const [loading, setLoading] = useState(true);
  const [posting, setPosting] = useState(false);
  const [aviso, setAviso] = useState<Aviso>(null);

  const onIniciadaRef = useRef(onIniciada);
  onIniciadaRef.current = onIniciada;
  // El token va por ref: un refresh de sesión (RNF-24) no debe derribar la conexión viva
  // (solo se usa en el handshake de conexión/reconexión). El lobby puede esperar minutos
  // a que se cumplan los mínimos de participación, más que el ciclo de refresh.
  const tokenRef = useRef(token);
  tokenRef.current = token;

  const load = useCallback(async () => {
    const r = (await cargarLobby({ apiBaseUrl, token, partidaId, fetchImpl: undefined })) as LobbyResult;
    if (!r.ok) {
      setAviso({ variant: "error", texto: r.message ?? "No se pudo cargar el lobby." });
      return;
    }
    if (r.lobby.estado === "Iniciada") {
      onIniciadaRef.current();
      return;
    }
    setLobby(r.lobby);
    setInscrito(r.inscrito);
    setEsLider(r.esLider);
  }, [apiBaseUrl, token, partidaId]);

  useEffect(() => {
    (async () => {
      setLoading(true);
      await load();
      setLoading(false);
    })();
  }, [load]);

  // Hub: refetch en EnLobby, avisos terminales en Iniciada/Cancelada.
  const loadRef = useRef(load);
  loadRef.current = load;
  useEffect(() => {
    const hub = crearSesionHub(apiBaseUrl, () => tokenRef.current);
    hub.on("PartidaEnLobby", () => void loadRef.current());
    hub.on("PartidaIniciada", () => onIniciadaRef.current());
    hub.on("PartidaCancelada", (p: { motivo?: string }) =>
      setAviso({ variant: "error", texto: p?.motivo ? `Partida cancelada: ${p.motivo}` : "Partida cancelada." })
    );
    hub
      .start()
      .then(() => hub.invoke("SuscribirAPartida", partidaId))
      .catch(() => setAviso({ variant: "info", texto: "Sin conexión en vivo; usa recargar." }));
    return () => {
      void hub.stop().catch(() => {});
    };
  }, [apiBaseUrl, partidaId]);

  async function onAccion() {
    if (!lobby) return;
    setPosting(true);
    setAviso(null);
    const r = (await accionParticipacion({
      apiBaseUrl, token, partidaId, modalidad: lobby.modalidad, inscrito, fetchImpl: undefined,
    })) as AccionResult;
    setPosting(false);
    if (!r.ok) {
      setAviso({ variant: "error", texto: r.message ?? "No se pudo completar la acción." });
      return;
    }
    setAviso({ variant: "success", texto: inscrito ? "Participación cancelada." : "¡Listo! Estás dentro." });
    await load();
  }

  const labelAccion = lobby?.modalidad === "Equipo"
    ? (inscrito ? "Cancelar preinscripción del equipo" : "Preinscribir mi equipo")
    : (inscrito ? "Cancelar mi inscripción" : "Inscribirme");

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <ScreenHeader title={nombre} subtitle="Lobby de la partida" />
      {aviso ? <Notice variant={aviso.variant}>{aviso.texto}</Notice> : null}
      {loading ? <ActivityIndicator color={colors.primaryBright} style={styles.spinner} /> : null}
      {lobby ? (
        <Card style={styles.card}>
          <AppText variant="bodyStrong">{lobby.modalidad}</AppText>
          <AppText>
            Inscritos: {lobby.inscritosActivos} / max {lobby.maximosParticipacion} (min {lobby.minimosParticipacion})
          </AppText>
          {lobby.modalidad !== "Equipo" || esLider ? (
            <Button label={labelAccion} onPress={() => void onAccion()} disabled={posting} />
          ) : (
            <AppText>El líder gestiona la preinscripción del equipo.</AppText>
          )}
          <Button label="Recargar" variant="secondary" onPress={() => void load()} disabled={posting} />
        </Card>
      ) : null}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg },
  content: { padding: spacing.xl, gap: spacing.lg },
  spinner: { marginTop: spacing.lg },
  card: { gap: spacing.sm },
});
