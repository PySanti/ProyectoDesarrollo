#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"

# 1) Variables GLOBALES del repo (LAN_IP, puertos, Keycloak, BD)
if [[ -f ../../.env ]]; then
  set -a
  # shellcheck disable=SC1091
  source ../../.env
  set +a
fi

# 2) Valores propios del servicio (referencian ${VAR} de las globales)
if [[ -f .env ]]; then
  set -a
  # shellcheck disable=SC1091
  source .env
  set +a
fi

dotnet run --project "src/Umbral.IdentityService.Api/Umbral.IdentityService.Api.csproj"
