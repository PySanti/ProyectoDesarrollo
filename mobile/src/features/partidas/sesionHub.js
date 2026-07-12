// Hub de sesion via gateway (mismo hub que la web). El caller arranca/detiene la conexion.
import { HubConnectionBuilder } from "@microsoft/signalr";

export function sesionHubUrl(gatewayBaseUrl) {
  return `${gatewayBaseUrl.replace(/\/$/, "")}/operaciones-sesion/hubs/sesion`;
}

// getToken en vez de string: el token se lee en cada handshake, así un refresh
// (RNF-24) no obliga a reconectar la conexión viva.
export function crearSesionHub(gatewayBaseUrl, getToken) {
  return new HubConnectionBuilder()
    .withUrl(sesionHubUrl(gatewayBaseUrl), { accessTokenFactory: getToken })
    .withAutomaticReconnect()
    .build();
}
