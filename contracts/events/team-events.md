# Team Events

Owning publisher: Team Service

## Status

Event details must be completed feature by feature in the related SDD before implementation.

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
