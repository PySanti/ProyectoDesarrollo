import { useEffect, useState } from "react";
import { joinIndividualBdtFromScreen, loadPublishedBdtGamesFromScreen } from "./bdtPublishedGamesScreenModel.js";

export const bdtModalityFilters = ["Todas", "Individual", "Equipo"];

/**
 * Orquestación de la lista de BDT publicadas (HU-39/HU-41), extraída del antiguo controller para que la
 * UI sea presentacional (registro de juego). Carga al cambiar el filtro y permite la inscripción
 * individual (que deriva en una pantalla de espera con la posición en lobby). Lógica de red/validación
 * sigue en `bdtPublishedGamesScreenModel`/`bdtPublishedGamesFlow` (testeada aparte).
 *
 * @param {{ apiBaseUrl: string, token: string }} props
 */
export function useBdtPublishedGames({ apiBaseUrl, token }) {
  const [filter, setFilter] = useState("Todas");
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState(null);
  const [joinErrorMessage, setJoinErrorMessage] = useState(null);
  const [joiningPartidaId, setJoiningPartidaId] = useState(null);
  const [waitingData, setWaitingData] = useState(null);
  const [games, setGames] = useState([]);

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

  const joinIndividual = (game) => {
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

  return {
    filter,
    setFilter,
    loading,
    errorMessage,
    joinErrorMessage,
    joiningPartidaId,
    waitingData,
    games,
    joinIndividual,
  };
}
