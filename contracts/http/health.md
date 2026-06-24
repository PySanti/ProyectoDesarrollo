# Contract: GET /health (all backend services + gateway)

**Auth:** none (anonymous).
**Request:** `GET /health`
**Response:** `200 OK`, `Content-Type: application/json`

```json
{ "status": "healthy", "service": "<service-slug>" }
```

`service` is the service slug (`gateway`, `partidas`, `operaciones-sesion`, `puntuaciones`,
…). Used by compose/orchestration liveness checks. Business contracts for each service arrive
in their own slice.
