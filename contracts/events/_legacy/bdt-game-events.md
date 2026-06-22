# BDT Game Events

Owning publisher: BDT Game Service

## Status

Event details must be completed feature by feature in the related SDD before implementation.

For HU-10 specifically, no integration event publication is required for closure. Viewing published BDT games is a read-only query.

For HU-12 specifically, no integration event publication is required for closure. Filtering published BDT games by modality is a read-only query.

For HU-34 specifically, no RabbitMQ integration event publication is required for closure. Creating a BDT game is persisted by BDT Game Service and returned synchronously through `POST /api/bdt/games`. User-visible publication updates are deferred to HU-42 or HU-55 unless a later SDD introduces a SignalR/event contract.

For the HU-34 expected QR image decode helper, no RabbitMQ integration event and no SignalR/WebSocket message are required. Decoding a QR image for operator configuration is a synchronous helper through `POST /api/bdt/stages/expected-qr/decode` and does not mutate BDT state.

For HU-37 specifically, no integration event publication is required for closure. Viewing operator published BDT games is a read-only query.

For HU-39 specifically, no RabbitMQ integration event publication is required for closure. Joining an individual BDT is handled as a BDT Game Service state change persisted synchronously through `POST /api/bdt/games/{partidaId}/individual-inscriptions`. Operator lobby real-time updates are deferred to HU-42 or HU-55 unless a later SDD introduces a SignalR/event contract.

For HU-43 specifically, no RabbitMQ integration event publication is required for closure. Starting a BDT game is persisted synchronously through `POST /api/bdt/games/{partidaId}/start`. BDT Game Service then attempts a user-visible SignalR/WebSocket notification named `PartidaBDTIniciada` after the database commit.

For HU-44 specifically, no RabbitMQ integration event publication is required for closure. Viewing the active BDT stage is a read-only query through `GET /api/bdt/games/{partidaId}/active-stage`. The React Native mobile app may subscribe to the documented `PartidaBDTIniciada` SignalR/WebSocket message and refresh the active-stage query after receiving it. HU-44 must not introduce undocumented stage-close, stage-advance or cancellation event names.

For HU-45 specifically, no RabbitMQ integration event publication and no new SignalR/WebSocket message are required for closure. Uploading a QR treasure image is persisted synchronously through `POST /api/bdt/games/{partidaId}/stages/{etapaId}/treasures`, and the React Native mobile app relies on the HTTP response for the upload-received/unreadable-QR result. QR validation, stage close, stage advance and operator treasure supervision updates are deferred to their owning HUs unless documented before implementation.

## HU-43 SignalR/WebSocket message: PartidaBDTIniciada

Version:

- v1

Publisher:

- BDT Game Service

Consumers:

- React Native mobile participant app.
- React web operator views that subscribe to BDT state updates.

Trigger:

- A BDT game has been successfully started and its first stage has been activated by BDT Game Service.

SignalR hub path:

- `/hubs/bdt`

SignalR authorization and delivery scope:

- The `/hubs/bdt` hub requires an authenticated UMBRAL user.
- Current HU-43 hub access is limited to authenticated `Operador` and `Participante` users.
- Clients subscribe to a partida-specific group before receiving game-start updates for that partida.
- `PartidaBDTIniciada` is delivered to the `bdt-partida-{partidaId:N}` SignalR group, not broadcast to all connected clients.
- Hub methods and delivery scoping must not implement BDT business rules; backend HTTP commands and BDT Game Service state remain authoritative.
- `SubscribeToPartida(partidaId)` must authorize group membership before adding the connection to the partida group.
- Authenticated `Participante` users can subscribe only when BDT Game Service state shows they are registered/active in the requested BDT game.
- Authenticated `Operador` users can subscribe for operator supervision.
- Authentication alone is not enough for a participant to join another partida group.

Related HU:

- HU-43

Related requirement:

- RF-13
- RF-27
- RNF-03
- RNF-17

Payload:

```json
{
  "type": "PartidaBDTIniciada",
  "version": 1,
  "partidaId": "uuid",
  "estado": "Iniciada",
  "modalidad": "Individual | Equipo",
  "etapaActiva": {
    "etapaId": "uuid",
    "orden": 1,
    "tiempoLimiteSegundos": 300,
    "iniciadaEnUtc": "2026-01-01T00:00:00Z",
    "cierraEnUtc": "2026-01-01T00:05:00Z"
  },
  "occurredOnUtc": "2026-01-01T00:00:00Z"
}
```

Idempotency / deduplication:

- Consumers should treat `partidaId` plus `estado=Iniciada` plus `etapaActiva.etapaId` as the idempotency key for UI state replacement.

Real-time effect:

- Participants in lobby can transition to active-stage state or refresh active-stage data.
- Operator views can update BDT state to `Iniciada`.
- Unauthenticated clients do not receive HU-43 BDT start updates.
- Authenticated clients only receive the update after joining the corresponding partida group.
- Authenticated participants who are not registered/active in the corresponding partida must be rejected before joining the group and must not receive the update.

Dispatch failure behavior:

- SignalR/WebSocket dispatch happens after the database commit.
- A post-commit dispatch failure is logged by BDT Game Service.
- A post-commit dispatch failure does not roll back the start transition and does not change the HTTP response to `500`.
- Clients can recover by querying the current active-stage state through the HU-44 HTTP query when implemented.

History effect:

- Can be recorded by BDT Game Service history mechanisms when implemented. No separate Audit Service is introduced.

## HU-44 Real-time subscription behavior

Version:

- v1

Subscriber:

- React Native mobile participant app.

Documented message used:

- `PartidaBDTIniciada` from HU-43.

Related HU:

- HU-44

Related requirement:

- RF-13
- RF-28
- RNF-03
- RNF-17

Behavior:

- When mobile receives `PartidaBDTIniciada` for the current `partidaId`, it refreshes `GET /api/bdt/games/{partidaId}/active-stage`.
- If the refresh returns `200`, the mobile screen renders active-stage data using backend timestamps.
- If the refresh returns `409`, the mobile screen shows an unavailable-stage state and does not show the upload action.
- Mobile must not subscribe to undocumented stage-close, stage-advance or cancellation message names as part of HU-44.

Idempotency / deduplication:

- Repeated `PartidaBDTIniciada` messages for the same `partidaId` and `etapaActiva.etapaId` replace the current active-stage UI state.

Real-time effect:

- Keeps participant mobile active-stage screen synchronized after a BDT game starts.

History effect:

- none for HU-44; this is a read-side subscription behavior.

## HU-45 Event and Real-time Decision

Version:

- v1

Publisher:

- BDT Game Service

Related HU:

- HU-45

Related requirement:

- RF-13
- RF-28
- RF-29
- RF-30
- RF-35
- RF-36
- RNF-03
- RNF-16
- RNF-17
- RNF-18
- RNF-20

Decision:

- No RabbitMQ integration event is required for HU-45 closure.
- No new SignalR/WebSocket message is required for HU-45 closure.
- The synchronous HTTP response is the participant-facing result for upload receipt and QR readability processing.
- BDT Game Service may later record an internal history fact such as `TesoroQRSubido`, but that does not create a cross-service integration contract in HU-45.

Deferred behavior:

- Final QR comparison against `CodigoQREsperado` belongs to HU-46.
- Stage closing and stage advancement belong to HU-47.
- Operator treasure supervision real-time updates belong to their owning operator story when active.

History effect:

- The accepted upload attempt is persisted as BDT Game Service state/history data through the `TesoroQR` record. No separate Audit Service is introduced.

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
