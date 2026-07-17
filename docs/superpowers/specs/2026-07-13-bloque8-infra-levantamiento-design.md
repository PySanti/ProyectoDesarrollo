# Bloque 8 — Infraestructura y levantamiento E2E (RNF-10 pleno) — Design

**Fecha:** 2026-07-13 · **Rama:** `feature/bloque-7` (continúa ahí; el bloque es transversal) · **Estado:** APROBADO

## Problema

Faltan 5 días para la entrega y el stack completo nunca se ha levantado end-to-end. El enunciado exige (RNF-10, línea 110; tabla de tecnologías línea 182; rubro "Infraestructura y despliegue local" = 10%) que **frontend, backend, base de datos y mensajería corran en contenedores vía Docker Compose**. El compose actual (Bloque 6) cubre los 5 backend + postgres/rabbitmq/keycloak pero **no el frontend web**, no se rebuildea desde antes de los Bloques 7a-7f, no pasa `KEYCLOAK_CLIENT_SECRET` a identity (crear usuarios contra Keycloak fallaría) y no configura SMTP (el correo de credenciales 7f moriría silencioso, best-effort).

## Decisiones (fijadas con el usuario)

1. **Modo único: compose completo.** `docker compose -f infra/docker-compose.yml up -d --build` levanta TODO. Los `run-local.sh` quedan en el repo pero dejan de ser el flujo canónico documentado.
2. **Mobile en teléfono físico por LAN** (no emulador). La app Expo no se contenedoriza; corre en el teléfono con `expo start` contra el gateway/Keycloak por IP LAN.
3. **Correo real vía Gmail** (sin Mailpit). App password ya existente del usuario; usuarios de prueba con alias `usuario+algo@gmail.com`.

## Diseño

### 1. Topología final (9 contenedores + mobile fuera)

postgres · rabbitmq · keycloak · identity-service · partidas · operaciones-sesion · puntuaciones · gateway · **frontend (nuevo)**. Un solo comando levanta todo. Mobile: `expo start` en el host, app en el teléfono.

### 2. Contenedor frontend

`frontend/Dockerfile` multi-stage:
- Stage build: `node:20-alpine`, `npm ci` + `vite build`; `VITE_GATEWAY_BASE_URL`, `VITE_KEYCLOAK_URL`, `VITE_KEYCLOAK_REALM`, `VITE_KEYCLOAK_CLIENT_ID` como **build-args** (Vite los hornea en el bundle estático).
- Stage runtime: `nginx:alpine` sirviendo `dist/` con fallback SPA (`try_files $uri /index.html`). Host `:5173`.
- Los valores son del **navegador** (host): `http://localhost:5080` (gateway), `http://localhost:8080` (keycloak) — no nombres de la red docker.

### 3. LAN parametrizada (teléfono físico)

- `infra/.env` (gitignored) + `infra/.env.example` versionado, con `LAN_IP`.
- Compose interpola `${LAN_IP:-localhost}`: los 5 backend añaden `http://${LAN_IP}:8080/realms/UMBRAL-UCAB` a `KEYCLOAK_VALID_ISSUERS` (el teléfono obtiene tokens con issuer LAN — gotcha conocido del proyecto).
- `mobile/.env`: `EXPO_PUBLIC_KEYCLOAK_URL` y `EXPO_PUBLIC_GATEWAY_BASE_URL` con IP LAN (la var vieja `EXPO_PUBLIC_IDENTITY_API_BASE_URL` del `.env` local está obsoleta — alinear con `.env.example`). Redirect `umbral://auth` ya está en el realm import.
- CORS del gateway: `http://localhost:5173` (el navegador corre en el host; sumar `http://${LAN_IP}:5173` solo si hiciera falta abrir la web desde otra máquina — no es requisito).

### 4. Correo real vía Gmail

- `infra/.env`/`.env.example` ganan `SMTP_HOST`, `SMTP_PORT`, `SMTP_USE_STARTTLS`, `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_FROM_ADDRESS`, `SMTP_FROM_NAME` **+ `KEYCLOAK_CLIENT_SECRET`**.
- Compose interpola `SMTP_*` y `KEYCLOAK_CLIENT_SECRET` al contenedor identity (`DependencyInjection.cs:89-101` ya lee esos env; cero código nuevo).
- El usuario copia su app password existente de `services/identity-service/.env` a `infra/.env` (paso manual, 1 min; el agente nunca lee/mueve el secreto).

### 5. Verificación + documentación

- **Levantamiento desde cero** (volúmenes limpios) → 9 contenedores arriba y sanos. Primer rebuild desde Bloque 6: todo 7a-7f entra a docker por primera vez — el slice arregla lo que el build/arranque revele roto.
- **Smoke E2E manual:** login admin web → crear usuario → correo real llega a Gmail → crear partida → publicar → teléfono por LAN: login participante → ve partida publicada → se inscribe.
- **`GUIA-LEVANTAMIENTO.md` reescrita:** compose como único flujo canónico (setup `infra/.env` una vez + un comando), sección mobile/Expo, troubleshooting LAN (firewall, issuers, redirect URI).

## Manejo de errores

Healthchecks existentes (postgres/rabbitmq) + `depends_on` ya modelan el orden de arranque. Identity aplica esquema vía `EnsureCreated` + SQL idempotente; partidas/operaciones/puntuaciones vía `EF_MIGRATE_ON_STARTUP=true`. Fallo de SMTP sigue siendo best-effort (ADR-0012): se loguea, no tumba requests.

## Testing

Sin tests unitarios nuevos (es infraestructura). Verificación = build limpio + smoke E2E real documentado arriba. Las suites existentes siguen siendo el gate de regresión de código.

## Fuera de alcance

Contenedorizar mobile (imposible: corre en teléfono) · TLS/HTTPS · despliegue remoto/cloud · cambios de contratos o reglas de negocio · CI de imágenes docker (el CI actual de tests no cambia).
