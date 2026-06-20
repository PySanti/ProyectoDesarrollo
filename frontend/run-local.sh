#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"

# Carga las variables GLOBALES del repo (puertos, realm de Keycloak) para que
# frontend/.env las pueda expandir con ${VAR} al arrancar Vite.
if [[ -f ../.env ]]; then
  set -a
  # shellcheck disable=SC1091
  source ../.env
  set +a
fi

npm run dev
