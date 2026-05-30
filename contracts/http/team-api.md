# Team API Contract

Owning service: Team Service

## Status

Endpoint details must be completed feature by feature in the related SDD before implementation.

## Base path

Suggested base path, pending SDD confirmation:

```txt
/api/teams
```

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

Do not reuse the old incomplete `POST /api/teams` draft unless the corresponding HU SDD confirms its request, response and authorization.
