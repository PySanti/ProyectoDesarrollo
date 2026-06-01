# Team API Contract

Owning service: Team Service

## Status

Endpoint details must be completed feature by feature in the related SDD before implementation.

## Base path

Suggested base path, pending SDD confirmation:

```txt
/api/teams
```

## Team cardinality rule

All team endpoints must respect:

```txt
1 <= members <= 5
```

Examples:

- `POST /api/teams` creates a team with exactly one member: the creator.
- The creator is automatically the leader.
- Joining a team is rejected if the team already has 5 members.
- A team with one member is valid.
- Do not enforce a minimum of 2 members.

## Required endpoint template

```md
## <METHOD> <PATH>

Related HU:

- <HU-ID>

Related requirement:

- <RF/RNF/RB>

Authorization:

- <role or authenticated user condition>

Request:

```json
{}
```

Response:

```json
{}
```

Error responses:

| Status | Reason |
|---|---|
| 400 | Invalid request |
| 401 | Unauthenticated |
| 403 | Unauthorized |
| 404 | Resource not found |
| 409 | Business rule conflict |

Business rules:

- <rules>

Events published:

- <events or none>

Real-time updates:

- <updates or none>
```

## Notes

Do not reuse endpoint details unless the corresponding HU SDD confirms request, response, authorization and business rules.

## POST /api/teams

Related HU:

- HU-03

Related requirement:

- RF-07
- RNF-01
- RNF-02
- RNF-04
- RNF-06
- RNF-13
- RNF-14

Authorization:

- Authenticated participant (`Participante`).

Request:

```json
{
  "nombreEquipo": "Exploradores"
}
```

Response (`201 Created`):

```json
{
  "equipoId": "uuid",
  "nombreEquipo": "Exploradores",
  "codigoAcceso": "ABCD1234",
  "estado": "Activo",
  "liderUserId": "uuid",
  "integrantes": [
    {
      "userId": "uuid",
      "esLider": true
    }
  ]
}
```

Error responses:

| Status | Reason |
|---|---|
| 400 | Invalid request payload |
| 401 | Unauthenticated |
| 403 | Authenticated user without participant authorization/policy |
| 409 | User already belongs to active team or unique access-code generation exhausted |
| 500 | Persistence failure |

Business rules:

- Team creator is first member and leader.
- Team is created in active state.
- Team cardinality remains valid (`1..5`).
- A participant can belong to at most one active team.

Events published:

- `EquipoCreado` (v1).

Real-time updates:

- none.
