# Diseño — Bloque 2a: fundación gateway en clientes (RNF-21 parcial)

Fecha: 2026-07-07
Rama: `feature/bloque-2` (desde `develop` `aacdcd8`)
Fuente: auditoría de cobertura (`docs/04-sdd/auditorias/2026-07-06-auditoria-cobertura-requisitos.md`, Bloque 2)

## Contexto y problema

RNF-21 exige que **todo** el tráfico cliente↔backend pase por el gateway YARP. Hoy ningún cliente lo hace:

- Web: `frontend/src/api/identityApi.ts` → identity directo (`VITE_IDENTITY_API_BASE_URL`, :5000); `triviaApi.ts`/`bdtApi.ts` → servicios **viejos** (:5015/:5016).
- Mobile: 5 archivos de `mobile/src/features/teams/` + `mobile/src/config/env.ts` → identity directo; trivia/bdt → servicios viejos.
- Cero referencias a URL de gateway en `frontend/src/` y `mobile/src/`. Cero SignalR implementado.

El gateway ya expone las rutas necesarias con policies por rol (`/identity/*` con sub-rutas users/governance/teams) y **pasa el path sin transforms** — los servicios hospedan bajo su propio prefijo, así que re-apuntar clientes es swap de base URL, no de paths.

**Partición del Bloque 2** (acordada): 2a fundación gateway → 2b web config partida → 2c web operación en vivo → 2d mobile panel/inscripciones → 2e mobile gameplay → 2f UI Puntuaciones. Este spec cubre **solo 2a**.

## Decisiones (confirmadas con el usuario)

1. **Alcance mínimo:** solo re-apuntar identity/equipos vía gateway. SignalR se introduce en el primer slice que lo consuma (2c/2e/2f); RNF-24 (refresh 270s) es mini-slice aparte; el cliente HTTP unificado nace en 2b con el código nuevo.
2. **Enfoque A — swap de base URL:** sin refactor de capa API (los clientes trivia/bdt viejos mueren en 2b-2e; refactorizarlos ahora es trabajo muerto). Descartados B (refactor completo: churn sobre código condenado) y C (proxy Vite: sin equivalente mobile, inconsistente).
3. **Pantallas viejas siguen funcionando:** las vars `VITE_TRIVIA_API_BASE_URL`/`VITE_BDT_API_BASE_URL` (y equivalentes EXPO) quedan hasta que su slice las reemplace — deuda transitoria documentada.

## Diseño

### 1. Gateway — CORS (único cambio backend)

- El gateway **no tiene CORS**; el navegador bloquearía `:5173 → :5080`. Se agrega middleware CORS estándar: orígenes desde env `CORS_ALLOWED_ORIGINS` (CSV, default `http://localhost:5173`), cualquier método/header, `AllowCredentials` (lo necesitará SignalR en 2c/2f).
- Se **elimina** la policy CORS `FrontendDev` de identity (`services/identity-service/.../Program.cs`): tras 2a el browser no le habla directo, y si quedara viva YARP reenviaría sus headers CORS y el `Access-Control-Allow-Origin` saldría duplicado (el browser lo rechaza).
- Test de integración en gateway: preflight OPTIONS con `Origin` permitido devuelve `Access-Control-Allow-Origin` correcto.

### 2. Web

- `frontend/src/api/identityApi.ts`: base URL de `VITE_IDENTITY_API_BASE_URL` → **`VITE_GATEWAY_BASE_URL`**. Paths intactos (`/identity/...`).
- `triviaApi.ts` / `bdtApi.ts`: sin tocar.
- Tests que referencien la var vieja: actualizar al nuevo nombre.
- Keycloak: sin cambio (los clientes autentican directo con Keycloak, no vía gateway — doctrina CLAUDE.md).

### 3. Mobile

- `mobile/src/config/env.ts` + `features/teams/{createTeamApi,invitationsApi,inviteMemberApi,leaveTeamApi,transferLeadershipApi}.js`: `EXPO_PUBLIC_IDENTITY_API_BASE_URL` → **`EXPO_PUBLIC_GATEWAY_BASE_URL`**.
- Sin CORS (fetch nativo, no browser).
- LAN: verificar que el `run-local` del gateway escuche en `0.0.0.0` (teléfono físico debe alcanzarlo); compose ya lo hace (`http://+:8080`).

### 4. Env y guía

- `.env` raíz / `.env.example`: nueva `GATEWAY_PORT` (default 5080), propagada a `frontend/.env.example` (`VITE_GATEWAY_BASE_URL`) y `mobile/.env.example` (`EXPO_PUBLIC_GATEWAY_BASE_URL` con `${IP}`).
- Vars de identity directo se **eliminan** de ambos examples.
- `GUIA-LEVANTAMIENTO.md`: actualizar sección de levantamiento (gateway ahora obligatorio para web/mobile).

### 5. Fuera de alcance

- SignalR en clientes (2c/2e/2f).
- RNF-24 (scheduler refresh + modal).
- Cliente HTTP unificado (nace en 2b).
- Re-apuntar trivia/bdt viejos (mueren en 2b-2e).
- Rutas nuevas de gateway — la ruta admin de equipos con policy `Administrador` la necesitará el Bloque 4 (Mai); hoy `identity-teams` exige `Participante` y basta para el flujo mobile actual.

## Verificación

1. Suites frontend (`npm test`), mobile (`npm test`, `npm run typecheck`) y gateway (`dotnet test`) verdes.
2. E2E manual con stack vivo: login web admin → panel usuarios vía gateway (`:5080`); mobile → flujo equipos vía gateway.
3. CI del PR valida los 8 jobs.

## Criterios de aceptación

- Cero referencias a `VITE_IDENTITY_API_BASE_URL` / `EXPO_PUBLIC_IDENTITY_API_BASE_URL` en código y examples.
- Todo el tráfico identity/equipos de ambos clientes sale hacia `:5080` (gateway) y funciona igual que antes (usuarios, gobernanza, equipos, invitaciones).
- Preflight CORS vía gateway pasa; sin headers CORS duplicados en respuestas proxyadas.
- Pantallas trivia/bdt viejas siguen funcionando sin cambio.
- Cero cambios en contratos, dominio, HUs.
