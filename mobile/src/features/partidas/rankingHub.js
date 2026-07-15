// Hub de rankings de Puntuaciones (SP-4c) via gateway. El caller arranca/detiene la conexion.
import { HubConnectionBuilder } from "@microsoft/signalr";

export function rankingHubUrl(gatewayBaseUrl) {
  return `${gatewayBaseUrl.replace(/\/$/, "")}/puntuaciones/hubs/ranking`;
}

// getToken en vez de string: el token se lee en cada handshake, así un refresh
// (RNF-24) no obliga a reconectar la conexión viva.
export function crearRankingHub(gatewayBaseUrl, getToken) {
  return new HubConnectionBuilder()
    .withUrl(rankingHubUrl(gatewayBaseUrl), { accessTokenFactory: getToken })
    .withAutomaticReconnect()
    .build();
}
