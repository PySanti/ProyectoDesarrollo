// Fabrica delgada de la conexion SignalR al hub de sesion (Operaciones de Sesion), via gateway.
import { HubConnection, HubConnectionBuilder } from "@microsoft/signalr";

function resolveBaseUrl(): string {
  const value = import.meta.env.VITE_GATEWAY_BASE_URL as string | undefined;
  if (!value) {
    throw new Error("Missing VITE_GATEWAY_BASE_URL environment variable.");
  }
  return value.replace(/\/$/, "");
}

export function sesionHubUrl(): string {
  return `${resolveBaseUrl()}/operaciones-sesion/hubs/sesion`;
}

// getToken en vez de string: el token se lee en cada handshake, así un refresh
// (RNF-24) no obliga a reconectar la conexión viva.
export function crearSesionHub(getToken: () => string): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(sesionHubUrl(), { accessTokenFactory: getToken })
    .withAutomaticReconnect()
    .build();
}
