# BDT Game Events

Owning publisher: BDT Game Service

## Status

Event details must be completed feature by feature in the related SDD before implementation.

For HU-10 specifically, no integration event publication is required for closure. Viewing published BDT games is a read-only query.

For HU-12 specifically, no integration event publication is required for closure. Filtering published BDT games by modality is a read-only query.

## Required event template

```md
## <EventName>

Version:

- v1

Publisher:

- BDT Game Service

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
