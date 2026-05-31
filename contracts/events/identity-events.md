# Identity Events

Owning publisher: Identity Service

## Status

Event details must be completed feature by feature in the related SDD before implementation.

## UsuarioCreado

Version:

- v1

Publisher:

- Identity Service

Consumers:

- none by default

Trigger:

- Usuario local created after successful Keycloak creation and role assignment.

Related HU:

- Pending assignment in a future identity HU/SDD.

Related requirement:

- RF-01

Payload:

```json
{
  "userId": "uuid",
  "keycloakId": "string",
  "name": "string",
  "email": "string",
  "role": "Administrador | Operador | Participante",
  "status": "Activo",
  "occurredOnUtc": "2026-01-01T00:00:00Z"
}
```

Idempotency / deduplication:

- Use `userId` as idempotency key.

Real-time effect:

- none

History effect:

- Can be recorded by Identity Service audit/history mechanism.

## Required event template

```md
## <EventName>

Version:

- v1

Publisher:

- Identity Service

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
