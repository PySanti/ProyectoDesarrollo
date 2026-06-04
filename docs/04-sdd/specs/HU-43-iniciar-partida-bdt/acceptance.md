# HU-43 - Acceptance

## Acceptance Checklist

- [x] Operator can start a BDT game from React web.
- [x] Backend starts only games in `Lobby`.
- [x] Backend rejects start when minimum participation is not met.
- [x] Backend rejects manual start for strictly `Automatico` games.
- [x] Successful start persists `Iniciada` state.
- [x] Successful start activates the first stage.
- [x] Response includes active-stage timer timestamps from backend time.
- [x] Endpoint requires authenticated `Operador`.
- [x] SignalR/WebSocket update is emitted after successful persistence.
- [x] Post-commit SignalR/WebSocket dispatch failure is logged and does not roll back the persisted start transition or return `500`.
- [x] Controllers and real-time adapters contain no business rules.
- [x] SignalR hub requires authenticated users and does not expose BDT start updates through unauthenticated connections.
- [x] SignalR notification is scoped to authorized observers of the partida instead of broadcasting to all clients.
- [x] Authenticated participants not registered/active in the requested BDT game cannot subscribe to that game's SignalR group.
- [x] Registered/active participants can subscribe to their own BDT game's SignalR group.
- [x] Authenticated operators can subscribe to BDT partida groups for supervision.
- [x] Concurrent start attempts for the same BDT game are serialized so only one request succeeds and the others receive `409`.
- [x] Real-time payload is verified through the real SignalR adapter or an equivalent adapter-level test.
- [x] Contracts are updated before implementation.
- [x] Traceability matrix is updated after hardening implementation.

## Manual Verification Steps

1. Login in React web as `Operador`.
2. Open a BDT game in `Lobby` with enough registered participants.
3. Press start.
4. Confirm success state shows `Iniciada` and active stage data.
5. Confirm participant/mobile client can receive or refresh active-stage state.
6. Repeat with insufficient participants and confirm a clear conflict error.
7. Repeat with a non-operator user and confirm access is rejected.

## Automated Test Evidence

| Test type | Command / evidence | Status |
|---|---|---|
| Domain unit | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.UnitTests/Umbral.BdtGameService.UnitTests.csproj` | Passed: 71/71 |
| Application | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.UnitTests/Umbral.BdtGameService.UnitTests.csproj` | Passed: handler success, not found, conflict, realtime success, realtime failure and partida lock invocation |
| API integration | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj` | Passed: 94/94 |
| HU-43 integration | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --filter "FullyQualifiedName~Hu43"` | Passed: 17/17 |
| Contract | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.ContractTests/Umbral.BdtGameService.ContractTests.csproj` | Passed: 38/38 |
| PostgreSQL | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj` | Passed: `Hu43PostgresStartBdtGameTests` with isolated schema and concurrent-start conflict coverage |
| React web | `npm test -- --run` in `frontend/` | Passed: 32/32 |
| React web build | `npm run build` in `frontend/` | Passed |
| Real-time | `Hu43RealtimeIntegrationTests` through `/hubs/bdt` SignalR TestServer connection | Passed: unauthenticated hub rejection, partida-group scoped delivery, documented `PartidaBDTIniciada` payload, unauthorized participant subscription rejection, registered participant subscription and operator subscription. |

## Hardening Evidence for 10/10

| Gap | Required evidence before 10/10 |
|---|---|
| Hub authorization | Integration or component test proving `/hubs/bdt` rejects unauthenticated connections and accepts authenticated users according to project auth setup. |
| Partida-scoped delivery | SignalR adapter test or integration test proving `PartidaBDTIniciada` is sent only to the partida group/authorized observers, not `Clients.All`. |
| Subscription authorization | SignalR integration tests proving `SubscribeToPartida` rejects authenticated participants not registered/active in the requested BDT game, accepts registered/active participants and accepts operators for supervision. |
| Concurrent start safety | PostgreSQL/Npgsql concurrency test proving exactly one start succeeds and competing starts return `409`. |
| Payload fidelity | Adapter-level test proving the real emitted payload matches `contracts/events/bdt-game-events.md`. |

All HU-43 hardening gaps are now covered by automated evidence above.

## Traceability Status

| Field | Value |
|---|---|
| HU | HU-43 |
| Requirement | RF-04, RF-11, RF-13, RF-27, RF-31, RF-32, RF-35, RF-36, RNF-01, RNF-02, RNF-03, RNF-04, RNF-06, RNF-13, RNF-17 |
| Owning service | BDT Game Service |
| Client | React web |
| Contract | Implemented and contract-tested: `contracts/http/bdt-game-api.md`, `contracts/events/bdt-game-events.md` |
| Status | 10/10 / completed / tested / PostgreSQL-concurrency-verified / SignalR-auth-and-scoped-delivery-verified / subscription-authorization-verified / acceptance updated |
