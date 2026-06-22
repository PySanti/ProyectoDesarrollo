# Trivia Game Events

Owning publisher: Trivia Game Service

## Status

Event details must be completed feature by feature in the related SDD before implementation.

## Trivia scoring event rule

Trivia scoring events must not include time-derived score calculation.

Allowed scoring payload fields:

```json
{
  "participantId": "uuid",
  "questionId": "uuid",
  "assignedScore": 100,
  "scoreEarned": 100,
  "accumulatedScore": 300
}
```

Do not publish score fields like:

```json
{
  "remainingTime": 12,
  "totalTime": 30,
  "timeMultiplier": 0.4,
  "scoreEarned": 40
}
```

unless time fields are explicitly marked as telemetry and not used for scoring.

## Required event template

```md
## <EventName>

Version:

- v1

Publisher:

- Trivia Game Service

Consumers:

- <service or none>

Trigger:

- <business fact that already happened>

Related HU:

- <HU-ID>

Related requirement:

- <RF/RNF/RB>

Payload:

```json
{}
```

Idempotency / deduplication:

- <rule>

Real-time effect:

- <none or SignalR update>

History effect:

- <how this is recorded, if applicable>
```
