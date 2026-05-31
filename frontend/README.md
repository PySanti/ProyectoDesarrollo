# UMBRAL Frontend Web (HU-01)

React web client for administrator/operator flows.

Current implementation covers HU-01 create-user flow with Keycloak login.

## Setup

1. Copy env template:

Copy `env.example` into `.env`.

2. Configure values in `.env`:

- `VITE_IDENTITY_API_BASE_URL`
- `VITE_KEYCLOAK_URL`
- `VITE_KEYCLOAK_REALM`
- `VITE_KEYCLOAK_CLIENT_ID`

3. Install and run:

```bash
npm install
npm run dev
```

## Keycloak client requirements

Create/configure a public Keycloak client for the web app (example `umbral-web`):

- Access type: public
- Standard flow: enabled
- Valid redirect URIs: `http://localhost:5173/*`
- Web origins: `http://localhost:5173`

The authenticated user must include realm role `Administrador` to access HU-01 page.

## Scripts

- `npm run dev`
- `npm run test`
- `npm run build`
