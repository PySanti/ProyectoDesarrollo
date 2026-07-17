# UMBRAL Mobile (Expo)

React Native mobile client for participant flows.

## Migration State

The documentation doctrine has changed before full code migration. Current target services are `Identity`, `Partidas`, `Operaciones de Sesion` and `Puntuaciones`, behind the mandatory YARP gateway. This README may describe legacy implementation wiring where explicitly noted; use `docs/02-project-context/documentation-migration-status.md`, `mobile/mobile-context.md`, `docs/03-microservices/` and `contracts/` before planning new work.

Current scope includes:

- real Keycloak login (OIDC + PKCE);
- authenticated navigation shell;
- HU-03 create-team flow against Identity (`POST /api/teams`);
- HU-XX invitations inbox: participants receive, accept, and reject team invitations (`GET/POST /api/teams/invitations`);
- HU-XX invite-member flow for team leaders: load eligible participants and send invitations (`GET /api/teams/eligible-participants`, `POST /api/teams/invitations`).

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
- `EXPO_PUBLIC_GATEWAY_BASE_URL` — gateway base URL (teams, invitations, users via gateway)
- `EXPO_PUBLIC_APP_SCHEME`

Important:

- Do not use `localhost` for phone testing.
- Use host LAN IP (for example `http://192.168.1.20:5080`).
- The legacy team-service base URL variable has been retired; all team and invitation endpoints are served by Identity and reached through the gateway (`EXPO_PUBLIC_GATEWAY_BASE_URL`).

## 3) Keycloak client (`umbral-mobile`)

Create a public client in Keycloak:

- Client ID: `umbral-mobile`
- Standard Flow: enabled
- PKCE: enabled (`S256`)
- Valid Redirect URIs: `umbral://auth`

Assign realm role `Participante` to the test user.

## 5) Run app

```bash
npm install
npm run start
```

Open the QR in Expo Go on your phone.

## 6) Validate team flows on phone

### HU-03 Crear equipo
1. Login with Keycloak as `Participante`.
2. Open `Crear equipo` from Home.
3. Submit team name.
4. Verify success includes `equipoId`, `nombreEquipo`, `estado`, `liderUserId`.
5. Submit a second team with the same participant and verify `409` conflict.

### Invitations inbox
1. Login as a `Participante` who has received an invitation.
2. Navigate to `Invitaciones`.
3. Verify pending invitations appear with team name.
4. Accept or reject — confirm the list updates and a success message appears.

### Invite member (leader only)
1. Login as a `Participante` who is team leader.
2. Navigate to `Invitar miembro`.
3. Verify eligible participants (not already in a team) are listed.
4. Select one and press `Enviar invitacion`.
5. Verify the invitation is sent and a success message appears.
