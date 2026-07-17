// Fabrica delgada de la conexion SignalR al hub de rankings (Puntuaciones), via gateway.
import { HubConnection, HubConnectionBuilder } from "@microsoft/signalr";

function resolveBaseUrl(): string {
  const value = import.meta.env.VITE_GATEWAY_BASE_URL as string | undefined;
  if (!value) {
    throw new Error("Missing VITE_GATEWAY_BASE_URL environment variable.");
  }
  return value.replace(/\/$/, "");
}

export function rankingHubUrl(): string {
  return `${resolveBaseUrl()}/puntuaciones/hubs/ranking`;
}

// getToken en vez de string: el token se lee en cada handshake, así un refresh
// (RNF-24) no obliga a reconectar la conexión viva.
export function crearRankingHub(getToken: () => string): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(rankingHubUrl(), { accessTokenFactory: getToken })
    .withAutomaticReconnect()
    .build();
}
