import React, { useEffect, useState } from "react";
import { joinIndividualBdtFromScreen, loadPublishedBdtGamesFromScreen } from "./bdtPublishedGamesScreenModel.js";

const filters = ["Todas", "Individual", "Equipo"];
const emptyStyles = {};

export function BdtPublishedGamesScreenController({ apiBaseUrl, token, components, styles = emptyStyles }) {
  const [filter, setFilter] = useState("Todas");
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState(null);
  const [joinErrorMessage, setJoinErrorMessage] = useState(null);
  const [joiningPartidaId, setJoiningPartidaId] = useState(null);
  const [waitingData, setWaitingData] = useState(null);
  const [games, setGames] = useState([]);

  const { ActivityIndicator, Pressable, SafeAreaView, ScrollView, Text, View } = components;

  useEffect(() => {
    void loadPublishedBdtGamesFromScreen({
      apiBaseUrl,
      token,
      filter,
      setLoading,
      setErrorMessage,
      setGames,
    });
  }, [apiBaseUrl, token, filter]);

  const handleJoinIndividual = (game) => {
    if (joiningPartidaId) {
      return;
    }

    void joinIndividualBdtFromScreen({
      apiBaseUrl,
      token,
      game,
      setJoiningPartidaId,
      setJoinErrorMessage,
      setWaitingData,
    });
  };

  if (waitingData) {
    return React.createElement(
      SafeAreaView,
      { style: styles.safeArea },
      React.createElement(
        View,
        { style: styles.container },
        React.createElement(Text, { style: styles.title }, "Espera de BDT"),
        React.createElement(Text, { style: styles.description }, waitingData.mensaje),
        React.createElement(Text, { style: styles.cardLine }, `Partida: ${waitingData.nombre}`),
        React.createElement(Text, { style: styles.cardLine }, `Modalidad: ${waitingData.modalidad}`),
        React.createElement(Text, { style: styles.cardLine }, `Posicion en lobby: ${waitingData.posicionEnLobby}`),
      ),
    );
  }

  return React.createElement(
    SafeAreaView,
    { style: styles.safeArea },
    React.createElement(
      ScrollView,
      { contentContainerStyle: styles.container },
      React.createElement(Text, { style: styles.title }, "Busqueda del Tesoro"),
      React.createElement(Text, { style: styles.description }, "Partidas BDT publicadas en lobby para participantes."),
      React.createElement(
        View,
        { style: styles.filters },
        filters.map((item) =>
          React.createElement(
            Pressable,
            {
              key: item,
              accessibilityRole: "button",
              onPress: () => setFilter(item),
              style: [styles.filterButton, filter === item && styles.filterButtonActive],
            },
            React.createElement(Text, { style: [styles.filterText, filter === item && styles.filterTextActive] }, item),
          ),
        ),
      ),
      loading ? React.createElement(ActivityIndicator, { color: "#0b5fff" }) : null,
      errorMessage ? React.createElement(Text, { style: styles.error }, errorMessage) : null,
      joinErrorMessage ? React.createElement(Text, { style: styles.error }, joinErrorMessage) : null,
      !loading && !errorMessage && games.length === 0
        ? React.createElement(Text, { style: styles.empty }, "No hay partidas BDT publicadas para este filtro.")
        : null,
      games.map((game) =>
        React.createElement(
          View,
          { key: game.partidaId, style: styles.card },
          React.createElement(Text, { style: styles.cardTitle }, game.nombre),
          React.createElement(Text, { style: styles.cardLine }, `Modalidad: ${game.modalidad}`),
          React.createElement(Text, { style: styles.cardLine }, `Estado: ${game.estado}`),
          React.createElement(Text, { style: styles.cardLine }, `Area: ${game.areaBusqueda}`),
          React.createElement(Text, { style: styles.cardLine }, `Etapas: ${game.cantidadEtapas}`),
          game.modalidad === "Individual"
            ? React.createElement(
                Pressable,
                {
                  accessibilityRole: "button",
                  disabled: joiningPartidaId === game.partidaId,
                  onPress: () => handleJoinIndividual(game),
                  style: [styles.joinButton, joiningPartidaId === game.partidaId && styles.joinButtonDisabled],
                },
                React.createElement(
                  Text,
                  { style: styles.joinButtonText },
                  joiningPartidaId === game.partidaId ? "Uniendote..." : "Unirme individualmente",
                ),
              )
            : React.createElement(Text, { style: styles.cardLine }, "La union por equipo se gestiona con el lider."),
        ),
      ),
    ),
  );
}
