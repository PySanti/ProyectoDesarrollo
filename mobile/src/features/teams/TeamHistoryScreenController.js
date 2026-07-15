import React, { useEffect, useState } from "react";
import { loadTeamHistoryForScreen } from "./teamHistoryScreenModel.js";

export const teamHistoryEmptyStateMessage =
  "Aún no perteneces a ningún equipo. Todavía no tienes historial de equipos.";

const emptyStyles = {};

export function TeamHistoryScreenController({ apiBaseUrl, token, components, styles = emptyStyles }) {
  const [loading, setLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState(null);
  const [historial, setHistorial] = useState([]);

  const { ActivityIndicator, ScrollView, SafeAreaView, Text, View } = components;

  useEffect(() => {
    loadTeamHistoryForScreen({
      apiBaseUrl,
      token,
      setLoading,
      setErrorMessage,
      setHistorial,
    });
  }, [apiBaseUrl, token]);

  let body;
  if (loading) {
    body = React.createElement(ActivityIndicator, {
      color: styles.loadingIndicatorColor,
      style: styles.loadingIndicator,
    });
  } else if (errorMessage) {
    body = React.createElement(Text, { style: styles.error }, errorMessage);
  } else if (historial.length === 0) {
    body = React.createElement(Text, { style: styles.empty }, teamHistoryEmptyStateMessage);
  } else {
    body = React.createElement(
      View,
      { style: styles.list },
      historial.map((item, index) =>
        React.createElement(
          View,
          { key: `${item.equipoId ?? "equipo"}-${index}`, style: styles.item },
          React.createElement(Text, { style: styles.itemName }, item.nombreEquipo),
          React.createElement(Text, { style: styles.itemDate }, item.fechaRegistro),
        ),
      ),
    );
  }

  return React.createElement(
    SafeAreaView,
    { style: styles.safeArea },
    React.createElement(
      ScrollView,
      { contentContainerStyle: styles.container },
      React.createElement(Text, { style: styles.title }, "Historial de equipos"),
      React.createElement(
        Text,
        { style: styles.description },
        "Estos son los equipos a los que has pertenecido.",
      ),
      body,
    ),
  );
}
