# Session Events

## SessionCreated

Published by:

- Trivia Service
- Treasure Hunt Service

Consumed by:

- Audit Service
- Scoring Service

Payload example:

```json
{
  "eventId": "uuid",
  "eventType": "SessionCreated",
  "occurredAt": "datetime",
  "sessionId": "uuid",
  "mode": "TRIVIA | TREASURE_HUNT",
  "createdByUserId": "uuid"
}
```

## SessionStateChanged

Published by:

- Trivia Service
- Treasure Hunt Service

Consumed by:

- Audit Service
- Scoring Service

Payload example:

```json
{
  "eventId": "uuid",
  "eventType": "SessionStateChanged",
  "occurredAt": "datetime",
  "sessionId": "uuid",
  "previousState": "string",
  "newState": "string",
  "changedByUserId": "uuid"
}
```
