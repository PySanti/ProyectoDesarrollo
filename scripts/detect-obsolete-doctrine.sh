#!/usr/bin/env bash
# Detect ACTIVE obsolete-doctrine signatures (UMBRAL code-structure migration).
# Hits inside negations/historical/legacy/_legacy archives are acceptable; this
# script surfaces ALL hits for triage. Excludes legacy/scratch/build dirs.
# Usage: detect-obsolete-doctrine.sh [PATH ...]   (default: SP-1 surface)
set -uo pipefail
PATHS=("$@")
if [ ${#PATHS[@]} -eq 0 ]; then
  PATHS=(services/identity-service mobile/src contracts/http/identity-api.md contracts/events/identity-events.md infra/docker-compose.yml infra/keycloak)
fi
PAT='Umbral\.TeamService|Umbral\.TriviaGame|Umbral\.BdtGameService|\bteam-service\b|trivia-game-service|bdt-game-service|umbral_team|umbral_trivia_game|umbral_bdt_game|PartidaTrivia|PartidaBDT|CompetidorTrivia|ExploradorBDT|FormularioTrivia|CodigoAcceso|codigo de acceso|c[oó]digo de acceso|access code|join-by-code|EtapasGanadas'
rg -in --no-heading "$PAT" "${PATHS[@]}" \
  -g '!**/_legacy/**' -g '!**/_legacy-implementation-evidence/**' \
  -g '!**/bin/**' -g '!**/obj/**' -g '!**/node_modules/**' -g '!.git/**' -g '!.superpowers/**' \
  2>/dev/null
