# BDT Game API Contract

Owning service: BDT Game Service

## Status

Endpoint details must be completed feature by feature in the related SDD before implementation.

## GET /api/bdt/games/published

Related HU:

- HU-10
- HU-12

Related requirement:

- RF-05
- RF-35
- RNF-01
- RNF-04
- RNF-06
- RNF-13
- RNF-20

Authorization:

- Authenticated participant (`Participante`).

Query parameters:

| Name | Required | Values | Related HU | Description |
|---|---|---|---|---|
| `modalidad` | No | `Individual`, `Equipo` | HU-12 | When omitted, returns all published BDT games. |

Request:

- No body.

Response (`200 OK`):

```json
[
  {
    "partidaId": "uuid",
    "nombre": "string",
    "modalidad": "Individual | Equipo",
    "estado": "Lobby",
    "areaBusqueda": "string",
    "cantidadEtapas": 1
  }
]
```

Error responses:

| Status | Reason |
|---|---|
| 400 | Invalid `modalidad` query parameter |
| 401 | Unauthenticated |
| 403 | Authenticated user without participant authorization/policy |
| 500 | Persistence failure |

Business rules:

- Return only BDT games published for participants, represented by state `Lobby`.
- Without `modalidad`, return both individual and team BDT games.
- With `modalidad=Individual`, return only individual BDT games.
- With `modalidad=Equipo`, return only team BDT games.
- Listing and filtering do not create inscriptions and do not validate team leadership.

Events published:

- none required for HU-10 or HU-12 closure.

Real-time updates:

- none required for HU-10 or HU-12 closure.

## Required Endpoint Template

Use this template for future BDT endpoints after the related SDD confirms request, response, authorization and business rules.

```md
## <METHOD> <PATH>

Related HU:

- <HU-ID>

Related requirement:

- <RF/RNF/RB>

Authorization:

- <role or authenticated user condition>

Request:

- <request body or no body>

Response:

- <response body>

Error responses:

| Status | Reason |
|---|---|
| 400 | Invalid request |
| 401 | Unauthenticated |
| 403 | Unauthorized |
| 404 | Resource not found |
| 409 | Business rule conflict |
| 422 | QR/image cannot be processed when applicable |

Business rules:

- <rules>

Events published:

- <events or none>

Real-time updates:

- <updates or none>
```
