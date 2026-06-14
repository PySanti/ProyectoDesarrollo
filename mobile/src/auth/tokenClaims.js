function base64UrlDecode(value) {
  const normalized = value
    .replace(/-/g, "+")
    .replace(/_/g, "/")
    .padEnd(Math.ceil(value.length / 4) * 4, "=");

  if (typeof atob !== "function") {
    throw new Error("Global atob is not available in this runtime.");
  }

  return atob(normalized);
}

export function parseJwtPayload(token) {
  const parts = token.split(".");
  if (parts.length < 2) {
    throw new Error("Invalid access token format.");
  }

  return JSON.parse(base64UrlDecode(parts[1]));
}

// Solo los roles de aplicación de UMBRAL son relevantes para la UI. Se descartan los roles
// técnicos de Keycloak (default-roles-*, offline_access, uma_authorization, manage-account…),
// espejo del filtrado de la web (OBS-04). Mantiene el chip de rol limpio.
const APP_ROLES = new Set(["Administrador", "Operador", "Participante"]);

export function buildAuthUser(accessToken) {
  const payload = parseJwtPayload(accessToken);
  const sub = payload.sub;
  if (!sub) {
    throw new Error("Token does not contain sub claim.");
  }

  const rawRoles = Array.isArray(payload.realm_access?.roles) ? payload.realm_access.roles : [];
  const roles = rawRoles.filter((role) => APP_ROLES.has(role));
  const username = payload.preferred_username || payload.name || "unknown";
  return { sub, username, roles };
}

export function isJwtExpired(accessToken, clockSkewSeconds = 30) {
  const payload = parseJwtPayload(accessToken);
  if (typeof payload.exp !== "number") {
    return true;
  }

  const nowSeconds = Math.floor(Date.now() / 1000);
  return payload.exp <= nowSeconds + clockSkewSeconds;
}
