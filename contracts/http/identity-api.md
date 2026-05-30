# Identity API Contract

Owning service: Identity Service

## Status

Endpoint details must be completed feature by feature in the related SDD before implementation.

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

Business rules:

- <rules>

Events published:

- <events or none>

Real-time updates:

- <updates or none>
```
