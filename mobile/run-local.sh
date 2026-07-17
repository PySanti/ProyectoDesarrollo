#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"

# Carga las variables GLOBALES del repo (LAN_IP, puertos, realm de Keycloak).
if [[ -f ../.env ]]; then
  set -a
  # shellcheck disable=SC1091
  source ../.env
  set +a
fi

# Genera mobile/.env con valores LITERALES ya resueltos.
#
# IMPORTANTE: no se puede dejar ${VAR} dentro de mobile/.env. Expo/Babel
# (babel-preset-expo) construye un "modulo virtual .env" que reescribe los
# accesos a process.env.EXPO_PUBLIC_* y NO expande ${VAR}: se quedaria con el
# default (localhost), y la app intentaria abrir Keycloak en localhost desde el
# telefono -> "no se pudo cargar la pagina". Por eso resolvemos aqui, con las
# variables ya cargadas del .env raiz, y escribimos el archivo literal.
IP="${LAN_IP:-localhost}"
cat > .env <<EOF
# ARCHIVO GENERADO por run-local.sh a partir del .env raiz del repo.
# No lo edites a mano: cambia los valores en <repo>/.env (LAN_IP, puertos...).
REACT_NATIVE_PACKAGER_HOSTNAME=${IP}
EXPO_PUBLIC_KEYCLOAK_URL=http://${IP}:${KEYCLOAK_PORT:-8080}
EXPO_PUBLIC_KEYCLOAK_REALM=${KEYCLOAK_REALM:-UMBRAL-UCAB}
EXPO_PUBLIC_KEYCLOAK_CLIENT_ID=umbral-mobile
EXPO_PUBLIC_GATEWAY_BASE_URL=http://${IP}:${GATEWAY_PORT:-5080}
EXPO_PUBLIC_APP_SCHEME=umbral
EXPO_PUBLIC_AUTH_REDIRECT_URI=exp://${IP}:${METRO_PORT:-8081}/--/auth
EOF

npm start -c
