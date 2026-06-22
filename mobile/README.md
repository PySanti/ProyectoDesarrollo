# UMBRAL Mobile (Expo)

React Native mobile client for participant flows.

## Migration State

The documentation doctrine has changed before full code migration. Current target services are `Identity`, `Partidas`, `Operaciones de Sesion` and `Puntuaciones`, behind the mandatory YARP gateway. This README may describe legacy implementation wiring where explicitly noted; use `docs/02-project-context/documentation-migration-status.md`, `mobile/mobile-context.md`, `docs/03-microservices/` and `contracts/` before planning new work.

Current scope includes:

- real Keycloak login (OIDC + PKCE);
- authenticated navigation shell;
- Legacy HU-03 create-team flow against the previous implementation layout (`POST /api/teams`). Team ownership is now Identity in the target doctrine.

## 1) Prerequisites

- Node.js 20+
- Expo CLI (`npx expo` is enough)
- Android phone with Expo Go (or dev client)
- Same Wi-Fi network between phone and backend host

## 2) Environment

Copy `.env.example` into `.env` and set LAN URLs:

- `EXPO_PUBLIC_KEYCLOAK_URL`
- `EXPO_PUBLIC_KEYCLOAK_REALM`
- `EXPO_PUBLIC_KEYCLOAK_CLIENT_ID`
- `EXPO_PUBLIC_TEAM_API_BASE_URL`
- `EXPO_PUBLIC_APP_SCHEME`

Important:

- Do not use `localhost` for phone testing.
- Use host LAN IP (for example `http://192.168.1.20:5099`).

## 3) Keycloak client (`umbral-mobile`)

Create a public client in Keycloak:

- Client ID: `umbral-mobile`
- Standard Flow: enabled
- PKCE: enabled (`S256`)
- Valid Redirect URIs: `umbral://auth`

Assign realm role `Participante` to the test user.

## 4) Legacy team implementation authentication

The previous team implementation folder supports real Keycloak JWT validation using these values. This is legacy implementation wiring, not active service-boundary doctrine:

- `KEYCLOAK_BASE_URL`
- `KEYCLOAK_REALM`
- `KEYCLOAK_CLIENT_ID`
- `KEYCLOAK_VALID_AUDIENCES` (comma-separated, optional)

Example for mobile compatibility:

```env
KEYCLOAK_BASE_URL=http://localhost:8080
KEYCLOAK_REALM=UMBRAL-UCAB
KEYCLOAK_CLIENT_ID=team-service
KEYCLOAK_VALID_AUDIENCES=team-service,umbral-mobile
```

## 5) Run app

```bash
npm install
npm run start
```

Open the QR in Expo Go on your phone.

## 6) Validate HU-03 on phone

1. Login with Keycloak.
2. Open `HU-03 Crear equipo` from Home.
3. Submit team name.
4. Verify success includes `equipoId`, `codigoAcceso`, `estado`, `liderUserId`.
5. Submit a second team with the same participant and verify `409` conflict.
