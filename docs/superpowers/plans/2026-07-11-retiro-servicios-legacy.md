# Retiro Servicios Legacy (Bloque 3 Cobertura) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Retirar físicamente `trivia-game-service` y `bdt-game-service` (código, compose, env template, guía, cliente Keycloak de seeds) — cierra el Bloque 3 de la auditoría de cobertura.

**Architecture:** Borrado puro sin código nuevo: `git rm -r` de ambos árboles + edición de 4 archivos de config/docs. Nada activo los consume (auditoría de conformidad D9 CONFORME; CI no los compila).

**Tech Stack:** git, docker compose (solo validación de YAML), edición de markdown/JSON.

## Global Constraints

- Spec: `docs/superpowers/specs/2026-07-11-retiro-servicios-legacy-design.md` (258851c).
- Rama `feature/bloque-2`. Un commit, trailer exacto: `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- PROHIBIDO: `git stash/reset/checkout/restore/clean`. Solo `git rm -r <rutas exactas>`, `git add <rutas exactas>`, `git commit`, `rm -rf` SOLO de `services/trivia-game-service` y `services/bdt-game-service`.
- NO tocar: `docs/03-microservices/**` (pointers), `scripts/detect-obsolete-doctrine.sh` (patrones), docs históricos, ni ningún archivo fuera de los listados.

---

### Task 1: Borrado trackeado completo

**Files:**
- Delete: `services/trivia-game-service/` (árbol completo), `services/bdt-game-service/` (árbol completo)
- Modify: `infra/docker-compose.yml` (línea 3 + bloques `trivia-game-service:` y `bdt-game-service:`, líneas ~182-211)
- Modify: `.env.example` (líneas `TRIVIA_PORT=5015` y `BDT_PORT=5016`)
- Modify: `GUIA-LEVANTAMIENTO.md` (nota línea ~49; bloques legacy de las secciones bash y Powershell; + línea de drop opcional de DBs)
- Modify: `infra/keycloak/import/umbral-realm.json` (objeto de cliente `bdt-game-service`)

**Interfaces:** ninguna (borrado; nada queda que consuma lo borrado).

- [ ] **Step 1: Borrar los dos servicios**

```bash
cd /home/santiago/Escritorio/ProyectoDesarrollo
git rm -r -q services/trivia-game-service services/bdt-game-service
rm -rf services/trivia-game-service services/bdt-game-service   # residuo no trackeado (bin/obj/etc.)
```

Verificar: `git ls-files services/trivia-game-service services/bdt-game-service | wc -l` → `0`; `ls services/` → solo `identity-service`, `partidas`, `operaciones-sesion`, `puntuaciones` (más archivos sueltos si los hay).

- [ ] **Step 2: docker-compose.yml**

En `infra/docker-compose.yml`:
1. Eliminar la línea 3 completa: `# Legacy (in transit, dismantled in SP-3/SP-4): trivia-game-service, bdt-game-service.`
2. Eliminar el bloque `trivia-game-service:` completo (desde `  trivia-game-service:` hasta la línea anterior a `  bdt-game-service:`).
3. Eliminar el bloque `bdt-game-service:` completo (desde `  bdt-game-service:` hasta la línea anterior a `volumes:`).
4. `volumes:` (con `umbral-postgres-data:` y `umbral-keycloak-data:`) y todo lo demás quedan intactos.

Verificar: `docker compose -f infra/docker-compose.yml config -q` → exit 0, sin warnings de sintaxis; `grep -c "trivia-game\|bdt-game" infra/docker-compose.yml` → `0`.

- [ ] **Step 3: .env.example**

Eliminar exactamente estas 2 líneas de `.env.example` (raíz):

```
TRIVIA_PORT=5015
BDT_PORT=5016
```

Verificar: `grep -c "TRIVIA_PORT\|BDT_PORT" .env.example` → `0`.

- [ ] **Step 4: GUIA-LEVANTAMIENTO.md**

1. Reemplazar la línea:
   `- Los servicios legacy (\`trivia-game-service\`, \`bdt-game-service\`) no forman parte de este levantamiento (pendientes de retiro, Bloque 3).`
   por:
   `- Los servicios legacy (\`trivia-game-service\`, \`bdt-game-service\`) fueron retirados del repositorio (Bloque 3 de cobertura, 2026-07-11). Si tu volumen local de Postgres aún tiene las bases \`umbral_trivia_game\`/\`umbral_bdt_game\`, son residuo inocuo; opcional: \`docker exec -it umbral-postgres psql -U umbral -d umbral -c "DROP DATABASE umbral_trivia_game;"\` (ídem \`umbral_bdt_game\`).`
2. En la sección bash, eliminar los dos bloques de terminal legacy:
   ```
   # Terminal 3
   ./services/bdt-game-service/run-local.sh
   ```
   y
   ```
   # Terminal 4
   ./services/trivia-game-service/run-local.sh
   ```
3. En la sección Powershell, eliminar los dos bloques equivalentes (`cd ./services/bdt-game-service/` + `./run-local.ps1` con su línea `# Terminal 3`, y `cd ./services/trivia-game-service/` + `./run-local.ps1` con su `# Terminal 4`).
4. NO renumerar los demás terminales (los números son etiquetas, no orden estricto) y NO tocar la nota histórica "Desde el Bloque 2d, el mobile ya no usa los servicios trivia/bdt viejos…".

Verificar: `grep -n "trivia-game\|bdt-game" GUIA-LEVANTAMIENTO.md` → solo la nota de retirados del punto 1 y la nota histórica del Bloque 2d.

- [ ] **Step 5: umbral-realm.json**

En `infra/keycloak/import/umbral-realm.json`, dentro del array `"clients"`, eliminar el objeto completo:

```json
    {
      "clientId": "bdt-game-service",
      "name": "BDT Game Service (backend)",
      "enabled": true,
      "publicClient": false,
      "secret": "umbral-bdt-secret",
      "standardFlowEnabled": false,
      "directAccessGrantsEnabled": true,
      "serviceAccountsEnabled": true
    }
```

incluyendo la coma que lo separa del objeto anterior (`identity-service`), dejando JSON válido.

Verificar: `python3 -m json.tool infra/keycloak/import/umbral-realm.json > /dev/null && echo JSON-OK` → `JSON-OK`; `grep -c "bdt-game-service" infra/keycloak/import/umbral-realm.json` → `0`.

- [ ] **Step 6: Verificación global**

```bash
git grep -lE "trivia-game-service|bdt-game-service|TRIVIA_PORT|BDT_PORT" -- . | sort
```

Expected: SOLO rutas bajo `docs/` (históricos + pointers de `docs/03-microservices/`), `GUIA-LEVANTAMIENTO.md` (las 2 notas permitidas) y `scripts/detect-obsolete-doctrine.sh`. Ningún archivo de `infra/`, `services/`, `gateway/`, `frontend/`, `mobile/`, `contracts/`, `.env.example` ni `.github/`.

- [ ] **Step 7: Commit**

```bash
git add infra/docker-compose.yml .env.example GUIA-LEVANTAMIENTO.md infra/keycloak/import/umbral-realm.json
git commit -m "chore(retiro-legacy): elimina trivia/bdt-game-service, compose, env y cliente keycloak (bloque 3 cobertura)"
```

(Los `git rm` del Step 1 ya están stageados.) Verificar: `git status --short` → vacío; `git show --stat HEAD | tail -3` muestra ~350 archivos borrados + 4 modificados.

---

### Cierre (controlador, no subagente)

- Limpieza local sin commit: quitar de `mobile/.env` las líneas `EXPO_PUBLIC_TEAM_API_BASE_URL`/`EXPO_PUBLIC_BDT_API_BASE_URL`/`EXPO_PUBLIC_TRIVIA_API_BASE_URL` y de `frontend/.env` las `VITE_TRIVIA_API_BASE_URL`/`VITE_BDT_API_BASE_URL`.
- Fila en `docs/04-sdd/traceability-matrix.md` (Bloque 3 de cobertura cerrado) + ledger.
- Review final del rango (task review + verificación del controller bastan si sale limpio; opus si aparece algo).
