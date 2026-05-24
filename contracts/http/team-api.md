# Team Api

## Endpoints

TODO: Define endpoints.

For each endpoint document:

- Method
- Path
- Request body
- Response body
- Error responses
- Authorization role
- Related HU
- Related requirement

# Team API Contract

## Base path

`/api/teams`

## POST /api/teams

Related HU:

- HU-02

Roles:

- Administrador

Request:

```json
{
  "name": "Equipo Alfa",
  "description": "Equipo de prueba"
}

