# Team Events

Owning publisher: Team Service

## Status

Event details must be completed feature by feature in the related SDD before implementation.

For HU-03 specifically, `EquipoCreado` is documented and emitted through the Team Service application port, but no RabbitMQ publisher/outbox is required for HU-03 closure.

For HU-04 specifically, no integration event publication is required for closure. Joining by access code is handled as an internal Team Service state change.

## Required event template

```md
## <EventName>

Version:

- v1

Publisher:

- Team Service

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

## EquipoCreado

Version:

- v1

Publisher:

- Team Service

Consumers:

- none required by HU-03

Trigger:

- Team is created successfully for an authenticated participant.

Related HU:

- HU-03

Related requirement:

- RF-07

Payload:

```json
{
  "equipoId": "uuid",
  "liderUserId": "uuid",
  "codigoAcceso": "ABCD1234",
  "occurredOnUtc": "2026-01-01T00:00:00Z"
}
```

Idempotency / deduplication:

- Use `equipoId` as idempotency key.

Real-time effect:

- none

History effect:

- Can be recorded by Team Service history/audit mechanism.
