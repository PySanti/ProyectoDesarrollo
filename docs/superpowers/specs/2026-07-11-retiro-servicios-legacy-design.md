# Retiro físico de servicios legacy (Bloque 3 de la auditoría de cobertura) — Design

- **Fecha:** 2026-07-11
- **Origen:** `docs/04-sdd/auditorias/2026-07-06-auditoria-cobertura-requisitos.md` §Bloque 3. Doctrina: CLAUDE.md §"Explicit non-services" y §"Migration status" — los cuatro servicios aprobados son los únicos físicos; el retiro del layout viejo es deuda de migración.
- **Rama:** `feature/bloque-2`. **Precondición verificada:** ningún cliente ni el gateway consume estos servicios (auditoría de conformidad 2026-07-11, D9 CONFORME, grep 0 activos); CI no los compila; los `run-local` de mobile ya no generan sus vars (remediación R4).

## Borrado trackeado (commits)

1. **`services/trivia-game-service/` y `services/bdt-game-service/` completos.** 346 archivos trackeados. `git rm -r` de ambos árboles + `rm -rf` del residuo no trackeado (bin/obj/node_modules; en disco 79M + 445M).
2. **`infra/docker-compose.yml`:** eliminar los bloques `trivia-game-service:` y `bdt-game-service:` (líneas ~182-211, incluyendo sus `build/environment/depends_on`) y el comentario de cabecera de la línea 3 ("Legacy (in transit, dismantled in SP-3/SP-4)…"). El bloque `volumes:` final y los 4 servicios doctrinales + infra (postgres/rabbitmq/keycloak) quedan intactos.
3. **`.env.example` (raíz):** eliminar `TRIVIA_PORT=5015` y `BDT_PORT=5016` (líneas ~28-29). Ninguna otra var se toca.
4. **`GUIA-LEVANTAMIENTO.md`:**
   - Línea ~49: la nota "(pendientes de retiro, Bloque 3)" pasa a afirmar que fueron retirados (Bloque 3, 2026-07-11).
   - Líneas ~102-137: eliminar los pasos de levantamiento de `bdt-game-service`/`trivia-game-service` (bloques `run-local.sh` y `cd services/...`).
   - La nota histórica de la línea ~79 ("Desde el Bloque 2d, el mobile ya no usa…") se conserva.
   - Añadir una línea de limpieza opcional: las DBs locales `umbral_trivia_game`/`umbral_bdt_game` pueden quedar en el volumen de Postgres; drop opcional (`DROP DATABASE ...`) si se desea.
5. **`infra/keycloak/import/umbral-realm.json`:** eliminar el objeto de cliente `"clientId": "bdt-game-service"` (línea ~110) completo. Solo afecta seeds frescos del realm; el realm vivo conserva el cliente hasta re-seed — inocuo, nada lo referencia (los `KEYCLOAK_VALID_AUDIENCES` que lo incluían viven solo en el bloque compose que se borra en el punto 2).

## Fuera de alcance (se conservan deliberadamente)

- `docs/03-microservices/services/trivia-game-service.md` y `bdt-game-service.md` — pointers/redirects documentales.
- `scripts/detect-obsolete-doctrine.sh` — sus menciones son patrones de detección.
- Referencias históricas en `docs/04-sdd/`, `docs/superpowers/`, ADRs y ledger — registros.
- Bases de datos legacy del volumen local de Postgres (residuo inocuo; drop documentado como opcional).

## Limpieza local (sin commit — archivos gitignored, la ejecuta el controlador)

- `mobile/.env`: eliminar las líneas `EXPO_PUBLIC_TEAM_API_BASE_URL` (:5099 — el ítem literal del audit-B3), `EXPO_PUBLIC_BDT_API_BASE_URL` (:5016) y `EXPO_PUBLIC_TRIVIA_API_BASE_URL` (:5015).
- `frontend/.env`: eliminar `VITE_TRIVIA_API_BASE_URL` y `VITE_BDT_API_BASE_URL`.
- Ningún código lee estas vars (auditado); los scripts `run-local` ya generan `.env` limpios tras R4.

## Verificación

- `docker compose -f infra/docker-compose.yml config -q` → exit 0 (YAML válido tras la edición).
- `git ls-files services/trivia-game-service services/bdt-game-service | wc -l` → 0; las carpetas no existen en disco.
- Grep de `trivia-game-service|bdt-game-service|TRIVIA_PORT|BDT_PORT` en árbol trackeado → solo docs históricos (`docs/**`), pointers de `docs/03-microservices/README.md` y `scripts/detect-obsolete-doctrine.sh`.
- `git status` limpio al cierre.
- Sin suites nuevas: ningún servicio activo, cliente ni contrato se toca. Smoke opcional barato: `dotnet test` del gateway (confirmación de que nada compila contra lo retirado — ya verificado por la auditoría, se corre solo si el ejecutor quiere evidencia extra).

## Cierre documental

- `docs/04-sdd/traceability-matrix.md`: fila "Retiro físico de servicios legacy (Bloque 3 de cobertura cerrado)".
- Ledger `.git/sdd/progress.md`: sección del slice.
- La auditoría de cobertura 2026-07-06 no se edita (documento histórico); la traceability es el registro vivo del cierre.
