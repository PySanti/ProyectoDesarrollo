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

## PATCH /api/teams/leadership

Related HU:

- HU-06

Related requirement:

- RF-08
- RF-35
- RF-36
- RNF-01
- RNF-02
- RNF-04
- RNF-06
- RNF-13
- RNF-14

Authorization:

- Authenticated participant (`Participante`) who is the current leader of their active team.

Request:

```json
{
  "nuevoLiderUserId": "uuid"
}
```

Response (`200 OK`):

```json
{
  "equipoId": "uuid",
  "liderAnteriorUserId": "uuid",
  "nuevoLiderUserId": "uuid",
  "equipoEstado": "Activo"
}
```

Error responses:

| Status | Reason |
|---|---|
| 400 | Invalid request payload |
| 401 | Unauthenticated |
| 403 | Authenticated user without participant authorization/policy |
| 404 | Participant has no active team |
| 409 | Actor is not current leader, target is not a member, target is current leader, team is not active, or no eligible target member exists |
| 500 | Persistence failure |

Business rules:

- Leadership is a Team Service domain condition, not a Keycloak role.
- The backend identifies the active team from the authenticated participant, not from a client-provided team id.
- Only the current leader can transfer leadership.
- The target new leader must be another current member of the same active team.
- A team with one member is valid, but cannot transfer leadership because no other member is eligible.
- Successful transfer keeps the team active and preserves membership cardinality (`1..5`).
- Successful transfer leaves exactly one leader: the selected new leader.
- HU-06 does not remove the former leader from the team; leaving remains HU-07.

Events published:

- none required for HU-06 closure.

Real-time updates:

- none.

## DELETE /api/teams/membership

Related HU:

- HU-07

Related requirement:

- RF-07
- RF-08
- RF-35
- RF-36
- RNF-01
- RNF-02
- RNF-04
- RNF-06
- RNF-13
- RNF-14

Authorization:

- Authenticated participant (`Participante`).

Request:

- Empty body.

Response (`200 OK`):

```json
{
  "userId": "uuid",
  "equipoId": "uuid",
  "resultado": "SalioDelEquipo | EquipoEliminado",
  "equipoEstado": "Activo | Eliminado"
}
```

Error responses:

| Status | Reason |
|---|---|
| 401 | Unauthenticated |
| 403 | Authenticated user without participant authorization/policy |
| 404 | Participant has no active team |
| 409 | Leader has other members and must transfer leadership first |
| 500 | Persistence failure |

Business rules:

- A non-leader participant can leave directly.
- A leader with other members cannot leave directly and must transfer leadership first through HU-06.
- A leader who is the only member can leave; the team is marked as `Eliminado`.
- Successful exit removes the active membership row so the participant can join another team later.
- Historical game participation records are not deleted or modified by this endpoint.

Events published:

- none required for HU-07 closure.

Real-time updates:

- none.

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

## POST /api/teams/join-by-code

Related HU:

- HU-04

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
  "codigoAcceso": "ABCD1234"
}
```

Response (`200 OK`):

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
    },
    {
      "userId": "uuid",
      "esLider": false
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
| 404 | Team not found by access code |
| 409 | User already belongs to active team, target team is full, or user is already in target team |
| 500 | Persistence failure |

Business rules:

- Access code must resolve to an active team.
- A participant can belong to at most one active team.
- Team cardinality remains valid (`1..5`).
- Joining is rejected if the target team already has 5 members.
- Joined participant is added as non-leader member.
- Concurrent join attempts must preserve the `1..5` cardinality invariant and map unique-membership persistence conflicts to business `409`.

Events published:

- none required for HU-04 closure.

Real-time updates:

- none.
