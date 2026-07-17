// Gameplay BDT del participante: etapa activa + tesoro QR (reintentos ilimitados) + pistas + ranking.
import React, { useCallback, useEffect, useState } from "react";
import { StyleSheet, View } from "react-native";
import { AppText, Button, Card, Notice } from "../../shared/ui";
import { spacing } from "../../shared/theme";
import { getEtapaActual, validarTesoro, getRankingJuego } from "./gameplayApi.js";
import { pickBdtTreasureImage, requestBdtTreasureImagePermission } from "../../permissions/bdtTreasureImagePicker.js";
import { Countdown, RankingTable, type RankingEntrada } from "./liveShared";
import { idsDeCompetidores } from "./liveLabels.js";
import { useNombres } from "../shared/useNombres.js";

type Etapa = {
  etapaId: string;
  orden: number;
  areaBusqueda: string;
  fechaActivacion: string;
  tiempoLimiteSegundos: number;
};

export type Pista = { texto: string; timestampUtc: string };

type EtapaResult =
  | { ok: true; etapa: Etapa }
  | { ok: false; type: string; message?: string };
type TesoroResult =
  | { ok: true; data: { resultado: string; gano: boolean; cerroEtapa: boolean; puntaje?: number | null } }
  | { ok: false; type: string; message?: string };
type RankingResult =
  | { ok: true; ranking: { entradas: RankingEntrada[] } }
  | { ok: false; type: string; message?: string };
type PermisoResult = { granted: boolean; unavailable: boolean };
type PickResult = { cancelled: true } | { image: { base64?: string } };

type Aviso = { variant: "info" | "error" | "success"; texto: string } | null;

type Props = {
  apiBaseUrl: string;
  token: string;
  partidaId: string;
  juegoId: string;
  refetchSignal: number; // bump = EtapaActivada/Cerrada/Ganada del hub
  resetSignal: number; // bump = EtapaActivada (nueva etapa → limpiar aviso del intento)
  miSub: string;
  pistas: Pista[];
  rankingPush: { juegoId: string; entradas: RankingEntrada[] } | null;
};

const MOTIVOS: Record<string, string> = {
  Invalido: "El QR no corresponde a esta etapa. Inténtalo de nuevo.",
  NoLegible: "No se pudo leer un QR en la imagen. Inténtalo de nuevo.",
  NoCorrespondeEtapaActiva: "Ese QR no es el de la etapa activa. Inténtalo de nuevo.",
};

export function BdtPlayPanel({
  apiBaseUrl, token, partidaId, juegoId, refetchSignal, resetSignal, miSub, pistas, rankingPush,
}: Props) {
  const [etapa, setEtapa] = useState<Etapa | null>(null);
  const [sinEtapa, setSinEtapa] = useState(false);
  const [entradas, setEntradas] = useState<RankingEntrada[]>([]);
  const nombreDe = useNombres(idsDeCompetidores(entradas), apiBaseUrl, token);
  const [posting, setPosting] = useState(false);
  const [aviso, setAviso] = useState<Aviso>(null);

  // Nueva etapa activada → limpiar el aviso del intento anterior.
  useEffect(() => {
    if (resetSignal > 0) setAviso(null);
  }, [resetSignal]);

  // Push SP-4c aditivo: ranking en vivo sin esperar señal de cierre.
  useEffect(() => {
    if (rankingPush && rankingPush.juegoId === juegoId) {
      setEntradas(rankingPush.entradas);
    }
  }, [rankingPush, juegoId]);

  const cargar = useCallback(async () => {
    const r = (await getEtapaActual(apiBaseUrl, token, partidaId, undefined)) as EtapaResult;
    if (r.ok) {
      setEtapa(r.etapa);
      setSinEtapa(false);
    } else if (r.type === "sin_etapa") {
      setEtapa(null);
      setSinEtapa(true);
    } else {
      setAviso({ variant: "error", texto: r.message ?? "No se pudo cargar la etapa." });
    }
    const rk = (await getRankingJuego(apiBaseUrl, token, partidaId, juegoId, undefined)) as RankingResult;
    if (rk.ok) setEntradas(rk.ranking.entradas ?? []);
  }, [apiBaseUrl, token, partidaId, juegoId]);

  useEffect(() => {
    void cargar();
  }, [cargar, refetchSignal]);

  async function onSubir(source: "camera" | "library") {
    setAviso(null);
    const permiso = (await requestBdtTreasureImagePermission()) as PermisoResult;
    if (!permiso.granted) {
      setAviso({
        variant: "info",
        texto: permiso.unavailable
          ? "La cámara/galería no está disponible en este dispositivo."
          : "Se necesita permiso de cámara o galería para subir el tesoro.",
      });
      return;
    }
    const pick = (await pickBdtTreasureImage(undefined, source)) as PickResult;
    if ("cancelled" in pick || !pick.image.base64) {
      return;
    }
    setPosting(true);
    const r = (await validarTesoro(apiBaseUrl, token, partidaId, pick.image.base64, undefined)) as TesoroResult;
    setPosting(false);
    if (!r.ok) {
      setAviso({ variant: "error", texto: r.message ?? "No se pudo validar el tesoro." });
      return;
    }
    if (r.data.gano) {
      setAviso({
        variant: "success",
        texto: `¡Etapa ganada!${r.data.puntaje != null ? ` +${r.data.puntaje} pts` : ""}`,
      });
      return;
    }
    setAviso({ variant: "error", texto: MOTIVOS[r.data.resultado] ?? "Validación fallida. Inténtalo de nuevo." });
  }

  const target = etapa
    ? new Date(new Date(etapa.fechaActivacion).getTime() + etapa.tiempoLimiteSegundos * 1000).toISOString()
    : null;

  return (
    <View style={styles.stack}>
      {aviso ? <Notice variant={aviso.variant}>{aviso.texto}</Notice> : null}
      {etapa ? (
        <Card style={styles.card}>
          <AppText variant="bodyStrong">Etapa {etapa.orden} — Búsqueda del tesoro</AppText>
          <AppText>Zona de búsqueda: {etapa.areaBusqueda}</AppText>
          {target ? <Countdown target={target} /> : null}
          <Button label="Subir QR con la cámara" disabled={posting} onPress={() => void onSubir("camera")} />
          <Button label="Subir QR desde la galería" variant="secondary" disabled={posting} onPress={() => void onSubir("library")} />
        </Card>
      ) : null}
      {sinEtapa ? (
        <Card style={styles.card}>
          <AppText>Esperando la siguiente etapa…</AppText>
        </Card>
      ) : null}
      {pistas.length > 0 ? (
        <Card style={styles.card}>
          <AppText variant="bodyStrong">Pistas recibidas</AppText>
          {pistas.map((p, i) => (
            <Notice key={`${p.timestampUtc}-${i}`} variant="info">{p.texto}</Notice>
          ))}
        </Card>
      ) : null}
      <Card style={styles.card}>
        <AppText variant="bodyStrong">Ranking del juego</AppText>
        <RankingTable entradas={entradas} resaltarId={miSub} nombreDe={nombreDe} />
      </Card>
    </View>
  );
}

const styles = StyleSheet.create({
  stack: { gap: spacing.lg },
  card: { gap: spacing.sm },
});
