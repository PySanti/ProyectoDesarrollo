# Trivia Game API Contract

Owning service: Trivia Game Service

## Status

Endpoint details must be completed feature by feature in the related SDD before implementation.

## Trivia scoring rule

Any endpoint that submits, validates or closes a Trivia answer must respect:

```txt
scoreEarned = question.assignedScore
```

Time must not be included in score calculation.

The timer can still be used to reject late answers.

## Team modality rule

When Trivia is team-based:

- the team is represented as the active Trivia participant;
- Team Service validates team existence, state and leadership;
- a team can have 1 to 5 members.

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
